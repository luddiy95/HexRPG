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
        float fixedRate = 0f; // �J�ڒ��Ɋ��荞�݂����������Ƃ��ɖ{���̑J�ڂ��ǂ̒��x��������

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

            // Graph����
            _graph = PlayableGraph.Create();

            Assert.IsNotNull(_animator);
            Assert.IsNotNull(_clips);
            Assert.IsTrue(_clips.All(x => x != null));

            // �S AnimaitonClip �p�� playable ��p�ӂ��A�S�� Mixer �łȂ��B
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

        void Play(string clip, float? duration = null) //TODO: Play�Ăяo���̒��O��Cancel����K�v������̂ł�(CancellationToken�̎󂯓n���������Ƃ���)->�ꉞ�Ή�����
        {
            if (_curPlayingIndex < 0)
            {
                // �ŏ��̑J��
                if (_prePlayingIndex >= 0) _mixer.SetInputWeight(_prePlayingIndex, 0);
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == clip);
                _mixer.SetInputWeight(_curPlayingIndex, 1);
                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);
                return;
            }
            else if (_playables[_curPlayingIndex].GetAnimationClip().name == clip)
            {
                // �������g�ɂ͑J�ڂ��Ȃ�
                return;
            }

            // Combat�ł����H
            var combatTimelineInfo = _combatTimelineInfoList.FirstOrDefault(info => info.CombatName == clip);
            if(combatTimelineInfo != null)
            {
                if (_curCombat != null) return;

                if(_cancellationTokenSource != null)
                {
                    // ���荞��(Locomotion->Locomotion�J�ڒ��̂݉\)
                    TokenCancel();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;
                }
                _cancellationTokenSource = new CancellationTokenSource();
                InnerPlayCombat(combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                return;
            }

            // Skill�ł����H
            var skillTimelineInfo = _skillTimelineInfoList.FirstOrDefault(info => info.SkillName == clip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                if (_cancellationTokenSource != null)
                {
                    // ���荞��(Locomotion->Locomotion�J�ڒ��̂݉\)
                    TokenCancel();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;
                }
                _cancellationTokenSource = new CancellationTokenSource();
                InnerPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
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

            //! �񓯊����\�b�h���s�� == CrossFade/Combat/Skill�҂����킹��
            if (_cancellationTokenSource != null)
            {
                //TODO: Locomotion->Locomotion�J�ڒ���Locomotion���荞�݂Ȃǂ�ActionStateController�Ő���ł��Ȃ����������炱���Ŋ��荞�݂̐��䂷��K�v����(?)
                var isDamagedClip = (_animationTypeMap.TryGetValue(clip, out AnimationType type) && type == AnimationType.Damaged);
                var isCombatSuspended = (_curCombat != null && clip == "Idle");

                if (isDamagedClip)
                {
                    //! ���荞��
                    //! InnerPlayCombat, InnerPlaySkill, InnerAnimationTransit����WaitWhile�A���\�b�h���̒�~
                    //! �L�����Z������token�������Ɏ����\�b�h�S��await�㑱�̏������L�����Z�������
                    TokenCancel();

                    if (_curCombat != null) FinishCombat(); // damaged�̏ꍇ�͂��̏u�ԂɏI��(InnerPlayCombat��await�㑱�����͌Ă΂�Ȃ�)
                    if (_curSkill != null) FinishSkill(); // damaged�̏ꍇ�͂��̏u�ԂɏI��(InnerPlaySkill��await�㑱�����͌Ă΂�Ȃ�)

                    // Fade���ł����荞��
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InnerAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
                }
                if (isCombatSuspended)
                {
                    TokenCancel();

                    // Fade���ł����荞��
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    _cancellationTokenSource.Token.Register(() => FinishCombat());
                    InnerAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
                }
            }
            else
            {
                // �����҂����킹�Ă��Ȃ�
                _cancellationTokenSource = new CancellationTokenSource();
                InnerAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
            }
        }

        async UniTask InnerPlayCombat(CombatTimelineInfo combatTimelineInfo, CancellationToken token)
        {
            _curCombat = combatTimelineInfo;

            for (int i = 0; i < _curCombat.TimelineClipInfoList.Count; i++)
            {
                var timelinClipInfo = _curCombat.TimelineClipInfoList[i];

                var transitionTime = (float)timelinClipInfo.BlendInDuration;
                if (i == 0) transitionTime = 0.25f; // ��

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

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
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
                if (i == 0) transitionTime = 0.25f; // ��

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

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
            TokenCancel();

            FinishSkill();
        }

        async UniTask InnerAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            var disposedPlayingIndex = _nextPlayingIndex;
            _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

            //! ���ɍĐ�����N���b�v�͍ŏ�(time = 0)����Đ�(�������Ȃ���LoopOff��Clip�֑J�ڂ���Ƃ���time���傫�����čĐ�����Ȃ��ꍇ������)
            _playables[_nextPlayingIndex].SetTime(0);
            _mixer.GetInput(_nextPlayingIndex).SetTime(0);

            await InnerCrossFade(transitionTime, disposedPlayingIndex, token);

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
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
            //TODO: Combat�I��
        }

        void FinishSkill()
        {
            _curSkill = null;
            _skillDirector.Stop();
            //TODO: Skill�I��
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
            public double Duration { get; set; } // Animation�S�̂̒���(s)(�{���̒�����Speed���|�������́A���ۂɂ����鎞��)
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
            OnUpdateLocomotionClip(); // CustomDuration��ݒ�

            _defaultDamagedDuration = _durationData.defaultDamagedDuration;
            _exitTimeToIdle = _durationData.exitTimeToIdle;
            _durationToIdle = _durationData.durationToIdle;
            OnUpdateDamagedClip(); // CustomDuration��ݒ�

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
            EditorGUILayout.LabelField("Current Clip�F", curClip);
            EditorGUILayout.LabelField("Combat is running�F", (_curCombat != null).ToString());
            EditorGUILayout.LabelField("Skill is running�F", (_curSkill != null).ToString());
            GUILayout.Space(10);
            //! Locomotion
            #region Locomotion
            using (new EditorGUILayout.VerticalScope(_skin.GetStyle("Block")))
            {
                GUILayout.Label("Locomotion", _skin.GetStyle("LabelLocomotion"));
                GUILayout.Label("DurationData�F" + "Member1DurationData", _skin.GetStyle("Description"));
                GUILayout.Space(10);

                GUI.enabled = (_graph.IsValid());
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Default Duration(s)�F");
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        _defaultLocomotionDuration = EditorGUILayout.FloatField(_defaultLocomotionDuration);
                        if (GUILayout.Button("Update", GUILayout.MaxWidth(60)))
                        {
                            _durationData.defaultLocomotionDuration = _defaultLocomotionDuration;
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();

                            // ���݂�before/after�A�j���[�V�����J�ڂŃJ�X�^��duration���Ȃ���΃f�t�H���gduration�𔽉f������
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
                    GUILayout.Label("�F");
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
                        // Fade�Ȃ���Play
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
                        // Fade�Ȃ���Play
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
                GUILayout.Label("DurationData�F" + "Member1DurationData", _skin.GetStyle("Description"));
                GUILayout.Space(10);

                GUI.enabled = (_graph.IsValid());
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Default Duration(s)�F");
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(100)))
                    {
                        _defaultDamagedDuration = EditorGUILayout.FloatField(_defaultDamagedDuration);
                        if (GUILayout.Button("Update", GUILayout.MaxWidth(60)))
                        {
                            _durationData.defaultDamagedDuration = _defaultDamagedDuration;
                            EditorUtility.SetDirty(_durationData);
                            AssetDatabase.SaveAssets();

                            // ���݂�before/after�A�j���[�V�����J�ڂŃJ�X�^��duration���Ȃ���΃f�t�H���gduration�𔽉f������
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
                    GUILayout.Label("�F");
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
                    GUILayout.Label("�F");
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
                        // Fade�Ȃ���Play
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
                        // Fade�Ȃ���Play
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
                GUILayout.Label("Playable�F" + "Member1ComboSword", _skin.GetStyle("Description"));
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
                GUILayout.Label("Playable�F" + "Member1Fire", _skin.GetStyle("Description"));
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
                //TODO: AttackCollider��disable�ɂ���
                //TODO: LocomotionBehaviour#SetSpeed(0)�ɂ���(?)
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

                        // combatTimelineInfoList�ɒǉ�
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
                                _mixer.AddInput(playable, 0, 0); // mixer�ɐڑ�
                            }
                        }
                        _combatTimelineInfoList.Add(new CombatTimelineInfo
                        {
                            CombatName = combatName,
                            TimelineClipInfoList = timelinClipInfoList
                        });

                        trackAsset.muted = true;
                    }

                    // �R���{����
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
                                        // Combat���f
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
                //TODO: Stop���ł͂Ȃ��Đ����̃G�t�F�N�g���I������܂ő�����H
                //TODO: Stop����disable�ɂ���
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

                        // skillTimelineInfoList�ɒǉ�
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
                                _mixer.AddInput(playable, 0, 0); // mixer�ɐڑ�
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