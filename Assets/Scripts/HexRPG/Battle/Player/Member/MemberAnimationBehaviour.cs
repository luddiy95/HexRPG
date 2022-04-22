using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    public class MemberAnimationBehaviour : AbstractAnimationBehaviour, IAnimationController
    {
        IAnimatorController _animatorController;
        ICombatSpawnObservable _combatSpawnObservable;
        ISkillSpawnObservable _skillSpawnObservable;

        [SerializeField] AnimationClip[] _clips;
        [SerializeField] DurationData _durationData;

        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;
        readonly ISubject<Unit> _onFinishDamaged = new Subject<Unit>();

        IObservable<Unit> IAnimationController.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        Dictionary<string, AnimationType> _animationTypeMap = new Dictionary<string, AnimationType>();

        // Playable
        PlayableGraph _graph;
        List<AnimationClipPlayable> _playables;
        AnimationMixerPlayable _mixer;

        int _allClipCount;

        struct TimelineClipInfo
        {
            public string ClipName { get; set; }
            public double Duration { get; set; } // Animation全体の長さ(s)(本来の長さにSpeedを掛けたもの、実際にかかる時間)
            public double Speed { get; set; }
            public double BlendInDuration { get; set; }
            public double BlendOutDuration { get; set; }
        }
        
        // Combat
        class CombatTimelineInfo
        {
            public string CombatName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        CombatTimelineInfo _combatTimelineInfo;
        CombatTimelineInfo _curCombat;

        // Skill
        class SkillTimelineInfo
        {
            public string SkillName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        List<SkillTimelineInfo> _skillTimelineInfos = new List<SkillTimelineInfo>();
        SkillTimelineInfo _curSkill;

        int _curPlayingIndex = -1, _nextPlayingIndex = -1;
        int _disposedPlayingIndex = -1;

        float rate = 0f;
        float fixedRate = 0f; // 遷移中に割り込みが発生したときに本来の遷移がどの程度だったか

        CancellationTokenSource _cancellationTokenSource;

        [Inject]
        public void Construct(
            IAnimatorController animatorController,
            ICombatSpawnObservable combatSpawnObservable,
            ISkillSpawnObservable skillSpawnObservable
        )
        {
            _animatorController = animatorController;
            _combatSpawnObservable = combatSpawnObservable;
            _skillSpawnObservable = skillSpawnObservable;
        }

        void IAnimationController.Init()
        {
            SetupGraph();
            var locomotionClipLength = AnimationExtensions.LocomotionClips.Length;
            for (int i = 0; i < locomotionClipLength; i++)
            {
                _animationTypeMap.Add(_playables[i].GetAnimationClip().name, AnimationType.Locomotion);
            }
            _animationTypeMap.Add(_playables[locomotionClipLength].GetAnimationClip().name, AnimationType.Damaged);

            SetupCombatAnimation(_combatSpawnObservable.Combat.Combat.PlayableAsset);
            Array.ForEach(_skillSpawnObservable.SkillList, skill => SetupSkillAnimation(skill.Skill.PlayableAsset));

            _allClipCount = _playables.Count;
        }

        #region AnimationPlayer

        //TODO: ここでdurationに引数を渡すのはeditor確認の場合(runtimeではdurationは全てdurationDataから取ってくるようにしたい)
        void IAnimationController.Play(string clip)
        {
            // 最初の遷移
            if (_curPlayingIndex < 0)
            {
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == clip);

                _mixer.SetInputWeight(_curPlayingIndex, 1);

                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);

                return;
            }

            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;

            float GetFadeLength()
            {
                var fadeLength = 0f;
                if (_animationTypeMap.TryGetValue(curClip, out AnimationType curAnimationType) == false ||
                    _animationTypeMap.TryGetValue(clip, out AnimationType nextAnimationType) == false) return _durationData.defaultDuration;

                switch (nextAnimationType)
                {
                    case AnimationType.Locomotion:
                        if(clip == "Idle")
                        {
                            switch (curAnimationType)
                            {
                                case AnimationType.Locomotion: // Move○○ -> Idle
                                case AnimationType.Damaged: // Damaged -> Idle
                                case AnimationType.Combat: // Combat -> Idle (中断のみ)
                                    fadeLength = _durationData.defaultBackToIdleDuration;
                                    var backToIdleDurationData = _durationData.backToIdleDurations.FirstOrDefault(data => data.clipBefore == curClip);
                                    if (backToIdleDurationData != null) fadeLength = backToIdleDurationData.duration;
                                    break;
                                default: fadeLength = _durationData.defaultDuration; break;
                            }
                        }
                        else
                        {
                            switch (curAnimationType)
                            {
                                case AnimationType.Locomotion:
                                    // Idle, Move○○ -> Move○○
                                    fadeLength = _durationData.defaultLocomotionDuration;
                                    var locomotionDurationData = _durationData.locomotionDurations.FirstOrDefault(data => data.clipBefore == curClip && data.clipAfter == clip);
                                    if (locomotionDurationData != null) fadeLength = locomotionDurationData.duration;
                                    break;
                                default: fadeLength = _durationData.defaultDuration; break;
                            }
                        }
                        break;
                    case AnimationType.Damaged:
                        fadeLength = _durationData.defaultDamagedDuration;
                        var damagedDurationData = _durationData.damagedDurations.FirstOrDefault(data => data.clipBefore == curClip);
                        if (damagedDurationData != null) fadeLength = damagedDurationData.duration;
                        break;
                    default: fadeLength = _durationData.defaultDuration; break;
                }
                return fadeLength;
            }

            if (_cancellationTokenSource == null)
            {
                // 遷移中などでない場合、自分自身には遷移しない
                if (_playables[_curPlayingIndex].GetAnimationClip().name == clip) return;

                // Combatですか？
                if (_combatTimelineInfo.CombatName == clip)
                {
                    if (_curCombat != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                    return;
                }

                // Skillですか？
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == clip);
                if (skillTimelineInfo != null)
                {
                    if (_curSkill != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                if (curClip == "Damaged" && clip == "Idle") _cancellationTokenSource.Token.Register(() => _onFinishDamaged.OnNext(Unit.Default));
                InternalAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget();
            }
            else
            {
                //! 割り込み(非同期メソッド実行中 == CrossFade(アニメーション遷移中), Combat/Skill待ち合わせ中)

                // _nextPlayingIndexへ遷移中、_nextPlayingIndexで割り込みしない
                if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == clip) return;

                // Locomotion->Locomotion遷移中は「Idle, Combat, Skill」割り込み可能
                var isCrossFadeBtwLocomotion =
                    (_animationTypeMap.TryGetValue(_playables[_curPlayingIndex].GetAnimationClip().name, out AnimationType type) && type == AnimationType.Locomotion) &&
                    (_animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type == AnimationType.Locomotion);
                if (isCrossFadeBtwLocomotion)
                {
                    // Combatですか？
                    if (_combatTimelineInfo.CombatName == clip)
                    {
                        if (_curCombat != null) return;

                        // 割り込み
                        TokenCancel();
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                        return;
                    }

                    // Skillですか？
                    var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == clip);
                    if (skillTimelineInfo != null)
                    {
                        if (_curSkill != null) return;

                        // 割り込み
                        TokenCancel();
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                        return;
                    }

                    if (clip == "Idle")
                    {
                        // 割り込み
                        TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                        return;
                    }
                }

                // Damaged
                var isDamagedClip = (_animationTypeMap.TryGetValue(clip, out type) && type == AnimationType.Damaged);
                if (isDamagedClip)
                {
                    TokenCancel();

                    if (_curCombat != null) FinishCombat();
                    if (_curSkill != null) FinishSkill();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget();
                    return;
                }

                // Combat中断
                var isCombatSuspended = (_curCombat != null && clip == "Idle");
                if (isCombatSuspended)
                {
                    TokenCancel();
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    _cancellationTokenSource.Token.Register(() => FinishCombat());
                    InternalAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget();
                    return;
                }
            }
        }

        async UniTask InternalPlayCombat(CombatTimelineInfo combatTimelineInfo, CancellationToken token)
        {
            _curCombat = combatTimelineInfo;

            for (int i = 0; i < _curCombat.TimelineClipInfoList.Count; i++)
            {
                var timelineClipInfo = _curCombat.TimelineClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0) fadeLength = _durationData.combatStartDuration;
                else if (timelineClipInfo.BlendInDuration >= 0) fadeLength = (float)timelineClipInfo.BlendInDuration;

                var blendOutDuration = timelineClipInfo.BlendOutDuration;
                if (blendOutDuration < 0) blendOutDuration = 0f;
                var exitTime = (float)(timelineClipInfo.Duration - blendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                await InternalCrossFade(timelineClipInfo.ClipName, fadeLength, token, timelineClipInfo.Speed);

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

        async UniTask InternalPlaySkill(SkillTimelineInfo skillTimelineInfo, CancellationToken token)
        {
            _curSkill = skillTimelineInfo;

            for (int i = 0; i < _curSkill.TimelineClipInfoList.Count; i++)
            {
                var timelineClipInfo = _curSkill.TimelineClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0) fadeLength = _durationData.skillStartDuration;
                else if (timelineClipInfo.BlendInDuration >= 0) fadeLength = (float)timelineClipInfo.BlendInDuration;

                var blendOutDuration = timelineClipInfo.BlendOutDuration;
                if (blendOutDuration < 0) blendOutDuration = 0f;
                var exitTime = (float)(timelineClipInfo.Duration - blendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                await InternalCrossFade(timelineClipInfo.ClipName, fadeLength, token, timelineClipInfo.Speed);

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

        async UniTask InternalAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            await InternalCrossFade(nextClip, transitionTime, token);

            // 割り込みがなかった場合のみここまで辿り着く
            TokenCancel();
        }

        async UniTask InternalCrossFade(string nextClip, float transitionTime, CancellationToken token, double speed = 1)
        {
            _disposedPlayingIndex = _nextPlayingIndex;
            _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

            //! _curPlayingIndexで割り込む場合
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip)
            {
                var inputCount = _mixer.GetInputCount();
                if (inputCount > _allClipCount) for (int i = inputCount - 1; i >= _allClipCount; i--) _mixer.SetInputCount(_allClipCount);
                if (_playables.Count > _allClipCount) _playables.RemoveRange(_allClipCount, _playables.Count - _allClipCount);

                // curPlayingIndexと同じClipのPlayableを作成しmixerに繋げる
                _playables.Add(AnimationClipPlayable.Create(_graph, _clips[_curPlayingIndex]));
                _mixer.AddInput(_playables[_allClipCount], 0, 0);

                _nextPlayingIndex = _allClipCount;
            }

            // 次に再生するクリップは最初(time = 0)から再生(こうしないとLoopOffのClipへ遷移するときにtimeが大きすぎて再生されない場合がある)
            _playables[_nextPlayingIndex].SetTime(0);
            _playables[_nextPlayingIndex].SetSpeed(speed);
            _mixer.GetInput(_nextPlayingIndex).SetTime(0);
            _mixer.GetInput(_nextPlayingIndex).SetSpeed(speed);

            token.Register(() =>
            {
                if (_disposedPlayingIndex >= 0)
                {
                    _mixer.SetInputWeight(_disposedPlayingIndex, 0);
                    _disposedPlayingIndex = -1;
                }
            });
            float waitTime = Time.timeSinceLevelLoad + transitionTime;

            await UniTask.WaitWhile(() =>
            {
                var diff = waitTime - Time.timeSinceLevelLoad;
                if (diff <= 0)
                {
                    _mixer.SetInputWeight(_curPlayingIndex, 0);

                    _mixer.SetInputWeight(_nextPlayingIndex, 0);
                    _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == _playables[_nextPlayingIndex].GetAnimationClip().name);
                    _mixer.SetInputWeight(_curPlayingIndex, 1);

                    if(_nextPlayingIndex >= _allClipCount)
                    {
                        _playables[_curPlayingIndex].SetTime(_playables[_nextPlayingIndex].GetTime());
                        _mixer.GetInput(_curPlayingIndex).SetTime(_mixer.GetInput(_nextPlayingIndex).GetTime());
                    }

                    _nextPlayingIndex = -1;
                    return false;
                }
                else
                {
                    rate = Mathf.Clamp01(diff / transitionTime);
                    if(_disposedPlayingIndex >= 0)
                    {
                        _mixer.SetInputWeight(_curPlayingIndex, fixedRate * rate);
                        _mixer.SetInputWeight(_disposedPlayingIndex, (1 - fixedRate) * rate);
                        _mixer.SetInputWeight(_nextPlayingIndex, 1 - rate);
                    }
                    else
                    {
                        _mixer.SetInputWeight(_curPlayingIndex, rate);
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

        #endregion

        void FinishCombat()
        {
            _curCombat = null;
            _onFinishCombat.OnNext(Unit.Default);
        }

        void FinishSkill()
        {
            _curSkill = null;
            _onFinishSkill.OnNext(Unit.Default);
        }

        #region Setup

        void SetupGraph()
        {
            _graph = PlayableGraph.Create();
            _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();
            _mixer = AnimationMixerPlayable.Create(_graph, _playables.Count, normalizeWeights: true);

            for (int i = 0; i < _playables.Count; ++i)
            {
                _mixer.ConnectInput(i, _playables[i], 0);
            }

            var output = AnimationPlayableOutput.Create(_graph, "AnimationPlayer", _animatorController.Animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        void SetupCombatAnimation(PlayableAsset playableAsset)
        {
            foreach (var trackAsset in (playableAsset as TimelineAsset).GetOutputTracks())
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

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Combat);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0);
                        }
                    }
                    _combatTimelineInfo = new CombatTimelineInfo
                    {
                        CombatName = playableAsset.name,
                        TimelineClipInfoList = timelinClipInfoList
                    };

                    trackAsset.muted = true;
                }
            }
        }

        void SetupSkillAnimation(PlayableAsset playableAsset)
        {
            //TODO: この辺はCombatの方で実行するべき
            /*
            _skillDirector.stopped += (obj) =>
            {
                //TODO: Stop時ではなく再生中のエフェクトが終了するまで続ける？
                //TODO: Stop時にdisableにする
            };
            */

            foreach (var trackAsset in (playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AnimationTrack)
                {
                    List<TimelineClip> clips = new List<TimelineClip>();
                    foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                    // skillTimelineInfoListに追加
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

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Skill);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0);
                        }
                    }
                    _skillTimelineInfos.Add(new SkillTimelineInfo
                    {
                        SkillName = playableAsset.name,
                        TimelineClipInfoList = timelinClipInfoList
                    });

                    trackAsset.muted = true;
                }
            }
        }

        #endregion

        void OnDestroy()
        {
            TokenCancel();
            _graph.Destroy();
        }

#if UNITY_EDITOR

        public override void SetupDamaged()
        {
            _graph = PlayableGraph.Create();
            _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();
            var damagedClip = _playables.First(playable => playable.GetAnimationClip().name == "Damaged").GetAnimationClip();
            var damagedToIdleEvent = new AnimationEvent[] {
                new AnimationEvent()
                {
                    time = damagedClip.length * _durationData.exitTimeToIdle,
                    functionName = "FadeToIdle"
                }
            };
            AnimationUtility.SetAnimationEvents(damagedClip, damagedToIdleEvent);
            _graph.Destroy();
        }

        public void FadeToIdle()
        {
            //TODO: Damaged終了通知
            (this as IAnimationController).Play("Idle");
        }

        [CustomEditor(typeof(MemberAnimationBehaviour))]
        public class MemberAnimationBehaviourInspector : Editor
        {
            private void OnEnable()
            {
            }

            private void OnDisable()
            {
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((MemberAnimationBehaviour)target).OnInspectorGUI();
            }
        }

#endif
    }
}
