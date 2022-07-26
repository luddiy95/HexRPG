using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AnimationTest
{
    using HexRPG.Playable;
    using HexRPG.Battle;

    public class AnimationPlayer : MonoBehaviour
    {
#if UNITY_EDITOR
        const int _locomotionCount = 9;

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip[] _clips;

        private PlayableGraph _graph;
        private List<AnimationClipPlayable> _playables;
        private AnimationMixerPlayable _mixer;

        int _prePlayingIndex = -1, _curPlayingIndex = -1, _nextPlayingIndex = -1;

        float rate = 0f;
        float fixedRate = 0f; // 遷移中に割り込みが発生したときに本来の遷移がどの程度だったか

        CombatTimelineInfo _curCombat = null;
        bool _isComboInputEnable = false;
        bool _isComboInputted = false;

        SkillTimelineInfo _curSkill = null;

        CancellationTokenSource _cancellationTokenSource;

        List<string> _animationClips = new List<string>();
        Dictionary<string, AnimationType> _animationTypeMap = new Dictionary<string, AnimationType>();

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            TokenCancel();

            _curPlayingIndex = -1;

            // Graph生成
            _graph = PlayableGraph.Create();

            Assert.IsNotNull(_animator);
            Assert.IsNotNull(_clips);
            Assert.IsTrue(_clips.All(x => x != null));

            // 全 AnimaitonClip 用に playable を用意し、全部 Mixer でつなぐ。
            _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();

            _mixer = AnimationMixerPlayable.Create(_graph, _playables.Count, normalizeWeights: true);
            for (int i = 0; i < _playables.Count; ++i)
            {
                _mixer.ConnectInput(i, _playables[i], 0);
            }

            var output = AnimationPlayableOutput.Create(_graph, "AnimationPlayer", _animator);
            output.SetSourcePlayable(_mixer);

            _graph.Play();
        }

        void Play(string clip, float? duration = null) //TODO: Play呼び出しの直前でCancelする必要があるのでは(CancellationTokenの受け渡しをちゃんとしろ)->一応対応した
        {
            if (_curPlayingIndex < 0)
            {
                // 最初の遷移
                if (_prePlayingIndex >= 0) _mixer.SetInputWeight(_prePlayingIndex, 0);
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == clip);
                _mixer.SetInputWeight(_curPlayingIndex, 1);
                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);
                return;
            }
            else if (_playables[_curPlayingIndex].GetAnimationClip().name == clip)
            {
                // 自分自身には遷移しない
                return;
            }

            // Combatですか？
            var combatTimelineInfo = _combatTimelineInfoList.FirstOrDefault(info => info.CombatName == clip);
            if(combatTimelineInfo != null)
            {
                if (_curCombat != null) return;

                if(_cancellationTokenSource != null)
                {
                    // 割り込み(Locomotion->Locomotion遷移中のみ可能)
                    TokenCancel();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;
                }
                _cancellationTokenSource = new CancellationTokenSource();
                InnerPlayCombat(combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                return;
            }

            // Skillですか？
            var skillTimelineInfo = _skillTimelineInfoList.FirstOrDefault(info => info.SkillName == clip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                if (_cancellationTokenSource != null)
                {
                    // 割り込み(Locomotion->Locomotion遷移中のみ可能)
                    TokenCancel();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;
                }
                _cancellationTokenSource = new CancellationTokenSource();
                InnerPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                return;
            }

            float GetFadeLength()
            {
                var fadeLength = 0f;
                if (duration != null)
                {
                    fadeLength = (float)duration;
                }
                else
                {
                    if (_animationTypeMap.TryGetValue(clip, out AnimationType type))
                    {
                        switch (type)
                        {
                            case AnimationType.Locomotion:
                                fadeLength = _durationData.defaultLocomotionDuration;
                                var LocomotiondurationData =
                                    _durationData.locomotionDurations.FirstOrDefault(
                                        data => data.clipBefore == _playables[_curPlayingIndex].GetAnimationClip().name && data.clipAfter == clip);
                                if (LocomotiondurationData != null) fadeLength = LocomotiondurationData.duration;
                                break;
                            case AnimationType.Damaged:
                                fadeLength = _durationData.defaultDamagedDuration;
                                var damagedDurationData =
                                    _durationData.damagedDurations.FirstOrDefault(
                                        data => data.clipBefore == _playables[_curPlayingIndex].GetAnimationClip().name);
                                if (damagedDurationData != null) fadeLength = damagedDurationData.duration;
                                break;
                        }
                    }
                }
                return fadeLength;
            }

            //! 非同期メソッド実行中 == CrossFade/Combat/Skill待ち合わせ中
            if (_cancellationTokenSource != null)
            {
                //TODO: Locomotion->Locomotion遷移中のLocomotion割り込みなどはActionStateControllerで制御できなさそうだからここで割り込みの制御する必要あり(?)
                var isDamagedClip = (_animationTypeMap.TryGetValue(clip, out AnimationType type) && type == AnimationType.Damaged);
                var isCombatSuspended = (_curCombat != null && clip == "Idle");

                if (isDamagedClip)
                {
                    //! 割り込む
                    //! InnerPlayCombat, InnerPlaySkill, InnerAnimationTransit内のWaitWhile、メソッド自体停止
                    //! キャンセルしたtokenを引数に持つメソッド全てawait後続の処理もキャンセルされる
                    TokenCancel();

                    if (_curCombat != null) FinishCombat(); // damagedの場合はその瞬間に終了(InnerPlayCombatのawait後続処理は呼ばれない)
                    if (_curSkill != null) FinishSkill(); // damagedの場合はその瞬間に終了(InnerPlaySkillのawait後続処理は呼ばれない)

                    // Fade中でも割り込む
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InnerAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                }
                if (isCombatSuspended)
                {
                    TokenCancel();

                    // Fade中でも割り込む
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    _cancellationTokenSource.Token.Register(() => FinishCombat());
                    InnerAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                }
            }
            else
            {
                // 何も待ち合わせていない
                _cancellationTokenSource = new CancellationTokenSource();
                InnerAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
            }
        }

        async UniTask InnerPlayCombat(CombatTimelineInfo combatTimelineInfo, CancellationToken token)
        {
            _curCombat = combatTimelineInfo;

            for (int i = 0; i < _curCombat.TimelineClipInfoList.Count; i++)
            {
                var timelinClipInfo = _curCombat.TimelineClipInfoList[i];

                var transitionTime = (float)timelinClipInfo.BlendInDuration;
                if (i == 0) transitionTime = 0.25f; // 仮

                var exitTime = (float)(timelinClipInfo.Duration - timelinClipInfo.BlendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                var disposedPlayingIndex = _nextPlayingIndex;
                _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == timelinClipInfo.ClipName);

                _playables[_nextPlayingIndex].SetTime(0);
                _playables[_nextPlayingIndex].SetSpeed(timelinClipInfo.Speed);
                _mixer.GetInput(_nextPlayingIndex).SetTime(0);
                _mixer.GetInput(_nextPlayingIndex).SetSpeed(timelinClipInfo.Speed);

                await InnerCrossFade(transitionTime, disposedPlayingIndex, token);

                await UniTask.WaitWhile(() =>
                {
                    var diff = exitWaitTime - Time.timeSinceLevelLoad;
                    return (diff > 0);
                }, cancellationToken: token);
            }

            // 割り込みがなかった場合のみここまで辿り着く
            TokenCancel();

            FinishCombat();
        }

        async UniTask InnerPlaySkill(SkillTimelineInfo skillTimelineInfo, CancellationToken token)
        {
            _curSkill = skillTimelineInfo;

            for (int i = 0; i < _curSkill.TimelineClipInfoList.Count; i++)
            {
                var timelinClipInfo = _curSkill.TimelineClipInfoList[i];

                var transitionTime = (float)timelinClipInfo.BlendInDuration;
                if (i == 0) transitionTime = 0.25f; // 仮

                var exitTime = (float)(timelinClipInfo.Duration - timelinClipInfo.BlendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                var disposedPlayingIndex = _nextPlayingIndex;
                _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == timelinClipInfo.ClipName);

                _playables[_nextPlayingIndex].SetTime(0);
                _playables[_nextPlayingIndex].SetSpeed(timelinClipInfo.Speed);
                _mixer.GetInput(_nextPlayingIndex).SetTime(0);
                _mixer.GetInput(_nextPlayingIndex).SetSpeed(timelinClipInfo.Speed);

                await InnerCrossFade(transitionTime, disposedPlayingIndex, token);

                await UniTask.WaitWhile(() =>
                {
                    var diff = exitWaitTime - Time.timeSinceLevelLoad;
                    return (diff > 0);
                }, cancellationToken: token);
            }

            // 割り込みがなかった場合のみここまで辿り着く
            TokenCancel();

            FinishSkill();
        }

        async UniTask InnerAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            var disposedPlayingIndex = _nextPlayingIndex;
            _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

            //! 次に再生するクリップは最初(time = 0)から再生(こうしないとLoopOffのClipへ遷移するときにtimeが大きすぎて再生されない場合がある)
            _playables[_nextPlayingIndex].SetTime(0);
            _mixer.GetInput(_nextPlayingIndex).SetTime(0);

            await InnerCrossFade(transitionTime, disposedPlayingIndex, token);

            // 割り込みがなかった場合のみここまで辿り着く
            TokenCancel();
        }

        async UniTask InnerCrossFade(float transitionTime, int disposedPlayingIndex, CancellationToken token)
        {
            float waitTime = Time.timeSinceLevelLoad + transitionTime;

            await UniTask.WaitWhile(() =>
            {
                var diff = waitTime - Time.timeSinceLevelLoad;
                if (diff <= 0)
                {
                    _prePlayingIndex = _curPlayingIndex;
                    _curPlayingIndex = _nextPlayingIndex;
                    _nextPlayingIndex = -1;
                    _mixer.SetInputWeight(_prePlayingIndex, 0);
                    _mixer.SetInputWeight(_curPlayingIndex, 1);
                    return false;
                }
                else
                {
                    rate = Mathf.Clamp01(diff / transitionTime);
                    if (disposedPlayingIndex == -1)
                    {
                        _mixer.SetInputWeight(_curPlayingIndex, rate);
                        _mixer.SetInputWeight(_nextPlayingIndex, 1 - rate);
                    }
                    else
                    {
                        _mixer.SetInputWeight(_curPlayingIndex, fixedRate * rate);
                        _mixer.SetInputWeight(disposedPlayingIndex, (1 - fixedRate) * rate);
                        _mixer.SetInputWeight(_nextPlayingIndex, 1 - rate);
                    }
                    return true;
                }
            }, cancellationToken: token);
        }

        void TokenCancel()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        void FinishCombat()
        {
            _curCombat = null;
            _combatDirector.Stop();
            //TODO: Combat終了
        }

        void FinishSkill()
        {
            _curSkill = null;
            _skillDirector.Stop();
            //TODO: Skill終了
        }

        void Dispose()
        {
            OnDestroy();
        }

        void OnDestroy()
        {
            _animationClips.Clear();
            _animationTypeMap.Clear();
            _combatTimelineInfoList.Clear();
            _skillTimelineInfoList.Clear();
            _curSkill = null;
            _curCombat = null;
            _curPlayingIndex = -1;
            _prePlayingIndex = -1;
            _nextPlayingIndex = -1;
            if (_graph.IsValid()) _graph.Destroy();
            if(_combatDirector != null) _combatDirector.Stop();
            if(_skillDirector != null) _skillDirector.Stop();
            TokenCancel();
        }

        GUISkin _skin;

        DurationData _durationData;

        // Locomotion
        string[] _locomotionClips;

        DurationData.LocomotionDurationData _customLocomotionDurationData;

        float _defaultLocomotionDuration;
        float _customLocomotionDuration;

        int _locomotionClipBeforeIndex;
        int _locomotionClipAfterIndex;

        // Damaged
        DurationData.DamagedDurationData _customDamagedDurationData;

        float _defaultDamagedDuration;
        float _customDamagedDuration;
        float _exitTimeToIdle;
        float _durationToIdle;

        int _damagedClipBeforeIndex;

        // Combat
        PlayableDirector _combatDirector;
        PlayableAsset _combatTimeline;

        public class CombatTimelineInfo
        {
            public string CombatName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        List<CombatTimelineInfo> _combatTimelineInfoList = new List<CombatTimelineInfo>();

        // Skill
        PlayableDirector _skillDirector;
        PlayableAsset _skillTimeline;

        public class SkillTimelineInfo
        {
            public string SkillName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        public struct TimelineClipInfo
        {
            public string ClipName { get; set; }
            public double Duration { get; set; } // Animation全体の長さ(s)(本来の長さにSpeedを掛けたもの、実際にかかる時間)
            public double Speed { get; set; }
            public double BlendInDuration { get; set; }
            public double BlendOutDuration { get; set; }
        }
        List<SkillTimelineInfo> _skillTimelineInfoList = new List<SkillTimelineInfo>();

        int _interruptClipIndex;

        internal void OnInspectorEnable()
        {
            _skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/Scenes/AnimationTest/GUISkin.guiskin");
            _durationData = Resources.Load<DurationData>("HexRPG/Battle/Member1DurationData");

            var clipName = "";
            for (int i = 0; i < _locomotionCount; i++)
            {
                clipName = _clips[i].name;
                if (_animationClips.Contains(clipName)) continue;
                _animationClips.Add(clipName);
                _animationTypeMap.Add(clipName, AnimationType.Locomotion);
            }
            clipName = _clips[_locomotionCount].name;
            if (!_animationClips.Contains(clipName))
            {
                _animationClips.Add(clipName);
                _animationTypeMap.Add(clipName, AnimationType.Damaged);
            }

            _locomotionClips = _animationClips
                .Where(clip => _animationTypeMap.TryGetValue(clip, out AnimationType type) && type == AnimationType.Locomotion).ToArray();
            _defaultLocomotionDuration = _durationData.defaultLocomotionDuration;
            OnUpdateLocomotionClip(); // CustomDurationを設定

            _defaultDamagedDuration = _durationData.defaultDamagedDuration;
            _exitTimeToIdle = _durationData.exitTimeToIdle;
            _durationToIdle = _durationData.durationToIdle;
            OnUpdateDamagedClip(); // CustomDurationを設定

            //TODO: D&D
            _combatTimeline = AssetDatabase.LoadAssetAtPath<PlayableAsset>("Assets/Timelines/HexRPG/Battle/Player/Member/Member1/Combat/Member1ComboSword.playable");
            _skillTimeline = AssetDatabase.LoadAssetAtPath<PlayableAsset>("Assets/Timelines/HexRPG/Battle/Player/Member/Member1/Skill/Member1Fire.playable");
        }

        internal void OnInspectorGUI()
        {
            GUILayout.Space(20);
            //! SetupPlayableGraph
            GUI.enabled = !_graph.IsValid();
            if (GUILayout.Button("SetupPlayableGraph"))
            {
                OnInspectorEnable();
                Initialize();
                Play("Idle");
            }
            GUI.enabled = true;
            GUILayout.Space(10);
            //! Current Clip
            var curClip = "null";
            if (_animationClips != null && _animationClips.Count > 0 && _curPlayingIndex >= 0) curClip = _animationClips[_curPlayingIndex];
            EditorGUILayout.LabelField("Current Clip：", curClip);
            EditorGUILayout.LabelField("Combat is running：", (_curCombat != null).ToString());
            EditorGUILayout.LabelField("Skill is running：", (_curSkill != null).ToString());
            GUILayout.Space(10);
            //! Locomotion
            #region Locomotion
            using (new EditorGUILayout.VerticalScope(_skin.GetStyle("Block")))
            {
                GUILayout.Label("Locomotion", _skin.GetStyle("LabelLocomotion"));
                GUILayout.Label("DurationData：" + "Member1DurationData", _skin.GetStyle("Description"));
                GUILayout.Space(10);

                GUI.enabled = (_graph.IsValid());
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Default Duration(s)：");
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        _defaultLocomotionDuration = EditorGUILayout.FloatField(_defaultLocomotionDuration);
                        if (GUILayout.Button("Update", GUILayout.MaxWidth(60)))
                        {
                            _durationData.defaultLocomotionDuration = _defaultLocomotionDuration;
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();

                            // 現在のbefore/afterアニメーション遷移でカスタムdurationがなければデフォルトdurationを反映させる
                            if (_customLocomotionDurationData == null) _customLocomotionDuration = _durationData.defaultLocomotionDuration;
                        }
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    int locomotionClipBeforeIndex = EditorGUILayout.Popup(_locomotionClipBeforeIndex, _locomotionClips, GUILayout.MaxWidth(110));
                    if (locomotionClipBeforeIndex != _locomotionClipBeforeIndex)
                    {
                        _locomotionClipBeforeIndex = locomotionClipBeforeIndex;
                        OnUpdateLocomotionClip();
                    }
                    GUILayout.Label(" -> ");
                    var locomotionClipAfterIndex = EditorGUILayout.Popup(_locomotionClipAfterIndex, _locomotionClips, GUILayout.MaxWidth(110));
                    if (locomotionClipAfterIndex != _locomotionClipAfterIndex)
                    {
                        _locomotionClipAfterIndex = locomotionClipAfterIndex;
                        OnUpdateLocomotionClip();
                    }
                    GUILayout.Label("：");
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        GUILayout.Label("Duration(s)");
                        var length = _durationData.locomotionDurations.Length;
                        _customLocomotionDuration = EditorGUILayout.FloatField(_customLocomotionDuration, GUILayout.MaxWidth(50));
                        if (GUILayout.Button("Save"))
                        {
                            if (_customLocomotionDurationData != null)
                            {
                                _customLocomotionDurationData.duration = _customLocomotionDuration;
                            }
                            else
                            {
                                Array.Resize(ref _durationData.locomotionDurations, length + 1);
                                _durationData.locomotionDurations[length] = new DurationData.LocomotionDurationData()
                                {
                                    clipBefore = _animationClips[_locomotionClipBeforeIndex],
                                    clipAfter = _animationClips[_locomotionClipAfterIndex],
                                    duration = _customLocomotionDuration
                                };
                                _customLocomotionDurationData = _durationData.locomotionDurations[length];
                            }
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();
                        }
                        GUI.enabled = (_graph.IsValid()) && (_customLocomotionDurationData != null);
                        if (GUILayout.Button("Clear"))
                        {
                            var list = _durationData.locomotionDurations.ToList();
                            list.Remove(_customLocomotionDurationData);
                            _durationData.locomotionDurations = list.ToArray();
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();
                            _customLocomotionDurationData = null;
                            _customLocomotionDuration = _durationData.defaultLocomotionDuration;

                        }
                        GUI.enabled = (_graph.IsValid());
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Play", EditorStyles.miniButtonLeft))
                    {
                        // FadeなしでPlay
                        _prePlayingIndex = _curPlayingIndex;
                        _curPlayingIndex = -1;
                        Play(_animationClips[_locomotionClipBeforeIndex]);
                    }
                    if (GUILayout.Button("Transit", EditorStyles.miniButtonMid))
                    {
                        Play(_animationClips[_locomotionClipAfterIndex], _customLocomotionDuration);
                    }
                    if (GUILayout.Button("Stop", EditorStyles.miniButtonRight))
                    {
                        // FadeなしでPlay
                        _prePlayingIndex = _curPlayingIndex;
                        _curPlayingIndex = -1;
                        Play("Idle");
                    }
                }
                GUI.enabled = true;
            }
            #endregion
            GUILayout.Space(10);
            //! Damaged
            #region Damaged
            using (new EditorGUILayout.VerticalScope(_skin.GetStyle("Block")))
            {
                GUILayout.Label("Damaged", _skin.GetStyle("LabelDamaged"));
                GUILayout.Label("DurationData：" + "Member1DurationData", _skin.GetStyle("Description"));
                GUILayout.Space(10);

                GUI.enabled = (_graph.IsValid());
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Default Duration(s)：");
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        _defaultDamagedDuration = EditorGUILayout.FloatField(_defaultDamagedDuration);
                        if (GUILayout.Button("Update", GUILayout.MaxWidth(60)))
                        {
                            _durationData.defaultDamagedDuration = _defaultDamagedDuration;
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();

                            // 現在のbefore/afterアニメーション遷移でカスタムdurationがなければデフォルトdurationを反映させる
                            if (_customDamagedDurationData == null) _customDamagedDuration = _durationData.defaultDamagedDuration;
                        }
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    int damagedClipBeforeIndex = EditorGUILayout.Popup(_damagedClipBeforeIndex, _animationClips.ToArray(), GUILayout.MaxWidth(110));
                    if (damagedClipBeforeIndex != _damagedClipBeforeIndex)
                    {
                        _damagedClipBeforeIndex = damagedClipBeforeIndex;
                        OnUpdateDamagedClip();
                    }
                    GUILayout.Label(" -> ");
                    GUILayout.Label("Damaged");
                    GUILayout.Label("：");
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        GUILayout.Label("Duration(s)");
                        var length = _durationData.damagedDurations.Length;
                        _customDamagedDuration = EditorGUILayout.FloatField(_customDamagedDuration, GUILayout.MaxWidth(50));
                        if (GUILayout.Button("Save"))
                        {
                            if (_customDamagedDurationData != null)
                            {
                                _customDamagedDurationData.duration = _customDamagedDuration;
                            }
                            else
                            {
                                Array.Resize(ref _durationData.damagedDurations, length + 1);
                                _durationData.damagedDurations[length] = new DurationData.DamagedDurationData()
                                {
                                    clipBefore = _animationClips[_damagedClipBeforeIndex],
                                    duration = _customDamagedDuration
                                };
                                _customDamagedDurationData = _durationData.damagedDurations[length];
                            }
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();
                        }
                        GUI.enabled = (_graph.IsValid()) && (_customDamagedDurationData != null);
                        if (GUILayout.Button("Clear"))
                        {
                            var list = _durationData.damagedDurations.ToList();
                            list.Remove(_customDamagedDurationData);
                            _durationData.damagedDurations = list.ToArray();
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();
                            _customDamagedDurationData = null;
                            _customDamagedDuration = _durationData.defaultDamagedDuration;

                        }
                        GUI.enabled = (_graph.IsValid());
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Damaged");
                    GUILayout.Label(" -> ");
                    GUILayout.Label("Idle");
                    GUILayout.Label("：");
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        GUILayout.Label("ExitTime");
                        var exitTimeToIdle = EditorGUILayout.FloatField(_exitTimeToIdle, GUILayout.MaxWidth(50));
                        if(exitTimeToIdle != _exitTimeToIdle)
                        {
                            _exitTimeToIdle = exitTimeToIdle;
                            UpdateDamagedToIdleEvent();
                        }
                        if (GUILayout.Button("Update"))
                        {
                            _durationData.exitTimeToIdle = _exitTimeToIdle;
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();
                        }
                        GUILayout.Label("Duration(s)");
                        _durationToIdle = EditorGUILayout.FloatField(_durationToIdle, GUILayout.MaxWidth(50));
                        if (GUILayout.Button("Update"))
                        {
                            _durationData.durationToIdle = _durationToIdle;
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Play", EditorStyles.miniButtonLeft))
                    {
                        // FadeなしでPlay
                        _prePlayingIndex = _curPlayingIndex;
                        _curPlayingIndex = -1;
                        Play(_animationClips[_damagedClipBeforeIndex]);
                    }
                    if (GUILayout.Button("Transit", EditorStyles.miniButtonMid))
                    {
                        Play("Damaged", _customDamagedDuration);
                    }
                    if (GUILayout.Button("Stop", EditorStyles.miniButtonRight))
                    {
                        // FadeなしでPlay
                        _prePlayingIndex = _curPlayingIndex;
                        _curPlayingIndex = -1;
                        Play("Idle");
                    }
                }
                GUI.enabled = true;
            }
            #endregion
            GUILayout.Space(10);
            //! Combat
            using (new EditorGUILayout.VerticalScope(_skin.GetStyle("Block")))
            {
                GUILayout.Label("Combat", _skin.GetStyle("LabelCombat"));
                GUILayout.Label("Playable：" + "Member1ComboSword", _skin.GetStyle("Description"));
                GUILayout.Space(10);
                if (GUILayout.Button("Setup"))
                {
                    SetupCombatPlayableDirector();
                }
                if (GUILayout.Button("Play"))
                {
                    if(_curCombat == null)
                    {
                        Play(_combatTimeline.name);
                        _combatDirector.Play();
                    }
                    else
                    {
                        if (_isComboInputEnable)
                        {
                            _isComboInputted = true;
                        }
                    }
                }
            }
            GUILayout.Space(10);
            //! Skill
            #region Skill
            using (new EditorGUILayout.VerticalScope(_skin.GetStyle("Block")))
            {
                GUILayout.Label("Skill", _skin.GetStyle("LabelSkill"));
                GUILayout.Label("Playable：" + "Member1Fire", _skin.GetStyle("Description"));
                GUILayout.Space(10);
                if (GUILayout.Button("Setup"))
                {
                    SetupSkillPlayableDirector();
                }
                GUI.enabled = (_curSkill == null);
                if (GUILayout.Button("Play"))
                {
                    Play(_skillTimeline.name);
                    _skillDirector.Play();
                }
                GUI.enabled = true;
            }
            #endregion
            GUILayout.Space(10);
            //! Interrupt
            GUI.enabled = (_graph.IsValid());
            using (new GUILayout.HorizontalScope())
            {
                _interruptClipIndex = EditorGUILayout.Popup(_interruptClipIndex, _animationClips.ToArray(), GUILayout.MaxWidth(110));
                if (GUILayout.Button("Interrupt"))
                {
                    Play(_animationClips[_interruptClipIndex]);
                }
            }
            GUI.enabled = true;
            GUILayout.Space(10);
            //! Dispose
            if (GUILayout.Button("Clear"))
            {
                OnInspectorDisable();
            }
        }

        internal void OnUpdateLocomotionClip()
        {
            _customLocomotionDuration = _durationData.defaultLocomotionDuration;
            _customLocomotionDurationData = _durationData.locomotionDurations.ToList()
                .FirstOrDefault(data => data.clipBefore == _animationClips[_locomotionClipBeforeIndex] && data.clipAfter == _animationClips[_locomotionClipAfterIndex]);
            if (_customLocomotionDurationData != null) _customLocomotionDuration = _customLocomotionDurationData.duration;
        }

        internal void OnUpdateDamagedClip()
        {
            _customDamagedDuration = _durationData.defaultDamagedDuration;
            _customDamagedDurationData = _durationData.damagedDurations.ToList()
                .FirstOrDefault(data => data.clipBefore == _animationClips[_damagedClipBeforeIndex]);
            if (_customDamagedDurationData != null) _customDamagedDuration = _customDamagedDurationData.duration;
        }

        internal void OnInspectorDisable()
        {
            if(_skillDirector != null) DestroyImmediate(_skillDirector.gameObject);
            if (_combatDirector != null) DestroyImmediate(_combatDirector.gameObject);
            Dispose();
        }

        internal void UpdateDamagedToIdleEvent()
        {
            var damagedClip = _playables.First(playable => playable.GetAnimationClip().name == "Damaged").GetAnimationClip();
            var damagedToIdleEvent = new AnimationEvent[] {
                new AnimationEvent()
                {
                    time = damagedClip.length * _exitTimeToIdle,
                    functionName = "FadeToIdle"
                }
            };
            AnimationUtility.SetAnimationEvents(damagedClip, damagedToIdleEvent);
        }

        public void FadeToIdle()
        {
            Play("Idle", _durationToIdle);
        }

        internal void SetupCombatPlayableDirector()
        {
            _combatDirector = new GameObject("Director", typeof(PlayableDirector)).GetComponent<PlayableDirector>();
            _combatDirector.transform.SetParent(transform);
            _combatDirector.playableAsset = _combatTimeline;
            _combatDirector.stopped += (obj) =>
            {
                _isComboInputEnable = false;
                //TODO: AttackColliderをdisableにする
                //TODO: LocomotionBehaviour#SetSpeed(0)にする(?)
            };

            foreach (var bind in _combatDirector.playableAsset.outputs)
            {
                if (bind.streamName == "Animation Track")
                {
                    _combatDirector.SetGenericBinding(bind.sourceObject, _animator);
                }
            }

            var combatName = _combatTimeline.name;
            if (!_combatTimelineInfoList.Any(info => info.CombatName == combatName))
            {
                foreach (var trackAsset in (_combatDirector.playableAsset as TimelineAsset).GetOutputTracks())
                {
                    if (trackAsset is AnimationTrack)
                    {
                        List<TimelineClip> clips = new List<TimelineClip>();
                        foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                        // combatTimelineInfoListに追加
                        List<TimelineClipInfo> timelinClipInfoList = new List<TimelineClipInfo>();
                        foreach (var clip in clips.OrderBy(clip => clip.start))
                        {
                            var clipName = clip.animationClip.name;

                            timelinClipInfoList.Add(new TimelineClipInfo
                            {
                                ClipName = clipName,
                                Duration = clip.duration,
                                Speed = clip.timeScale,
                                BlendInDuration = clip.blendInDuration,
                                BlendOutDuration = clip.blendOutDuration,
                            });

                            if (!_animationClips.Contains(clipName))
                            {
                                _animationClips.Add(clipName);
                                _animationTypeMap.Add(clipName, AnimationType.Combat);
                                var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                                _playables.Add(playable);
                                _mixer.AddInput(playable, 0, 0); // mixerに接続
                            }
                        }
                        _combatTimelineInfoList.Add(new CombatTimelineInfo
                        {
                            CombatName = combatName,
                            TimelineClipInfoList = timelinClipInfoList
                        });

                        trackAsset.muted = true;
                    }

                    // コンボ入力
                    /*
                    if (trackAsset is ComboInputEnableTrack)
                    {
                        foreach (var clip in trackAsset.GetClips())
                        {
                            var behaviour = (clip.asset as ComboInputEnableAsset).behaviour;
                            behaviour.OnComboInputEnable
                                .Subscribe(_ =>
                                {
                                    _isComboInputted = false;
                                    _isComboInputEnable = true;
                                })
                                .AddTo(this);
                            behaviour.OnComboInputDisable
                                .Subscribe(_ =>
                                {
                                    if (!_isComboInputted)
                                    {
                                        // Combat中断
                                        _animationPlayer.Play("Idle", 0.5f);
                                    }
                                    _isComboInputEnable = false;
                                })
                                .AddTo(this);
                        }
                    }
                    */
                }
            }
        }

        internal void SetupSkillPlayableDirector()
        {
            _skillDirector = new GameObject("Director", typeof(PlayableDirector)).GetComponent<PlayableDirector>();
            _skillDirector.transform.SetParent(transform);
            _skillDirector.playableAsset = _skillTimeline;
            _skillDirector.stopped += (obj) =>
            {
                //TODO: Stop時ではなく再生中のエフェクトが終了するまで続ける？
                //TODO: Stop時にdisableにする
            };

            foreach (var bind in _skillDirector.playableAsset.outputs)
            {
                if (bind.streamName == "Animation Track")
                {
                    _skillDirector.SetGenericBinding(bind.sourceObject, _animator);
                }
            }

            var skillName = _skillTimeline.name;
            if (!_skillTimelineInfoList.Any(info => info.SkillName == skillName))
            {
                foreach (var trackAsset in (_skillDirector.playableAsset as TimelineAsset).GetOutputTracks())
                {
                    if (trackAsset is AnimationTrack)
                    {
                        List<TimelineClip> clips = new List<TimelineClip>();
                        foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                        // skillTimelineInfoListに追加
                        List<TimelineClipInfo> timelinClipInfoList = new List<TimelineClipInfo>();
                        foreach(var clip in clips.OrderBy(clip => clip.start))
                        {
                            var clipName = clip.animationClip.name;

                            timelinClipInfoList.Add(new TimelineClipInfo
                            {
                                ClipName = clipName,
                                Duration = clip.duration,
                                Speed = clip.timeScale,
                                BlendInDuration = clip.blendInDuration,
                                BlendOutDuration = clip.blendOutDuration,
                            });

                            if (!_animationClips.Contains(clipName))
                            {
                                _animationClips.Add(clipName);
                                _animationTypeMap.Add(clipName, AnimationType.Skill);
                                var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                                _playables.Add(playable);
                                _mixer.AddInput(playable, 0, 0); // mixerに接続
                            }
                        }
                        _skillTimelineInfoList.Add(new SkillTimelineInfo
                        {
                            SkillName = skillName,
                            TimelineClipInfoList = timelinClipInfoList
                        });

                        trackAsset.muted = true;
                    }
                }
            }
        }

        [CustomEditor(typeof(AnimationPlayer))]
        public class AnimationPlayerInspector : Editor
        {
            private void OnEnable()
            {
                ((AnimationPlayer)target).OnInspectorEnable();
            }

            private void OnDisable()
            {
                ((AnimationPlayer)target).OnInspectorDisable();
            }

            public override bool RequiresConstantRepaint()
            {
                return true;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((AnimationPlayer)target).OnInspectorGUI();
            }
        }

#endif
    }
}