using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Timeline;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public class AnimationBehaviour : AbstractAnimationBehaviour
    {
        protected IProfileSetting _profileSetting;
        IDieSetting _dieSetting;
        IAnimatorController _animatorController;
        ICombatSpawnObservable _combatSpawnObservable;
        ISkillSpawnObservable _skillSpawnObservable;

        protected readonly ISubject<Unit> _onFinishDamaged = new Subject<Unit>();
        
        protected readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();
        protected readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        protected DurationDataContainer _durationDataContainer;

        // Playable
        protected PlayableGraph _graph;
        protected List<AnimationClipPlayable> _playables;
        protected AnimationMixerPlayable _mixer;

        [SerializeField] protected AnimationClip[] _clips;
        protected Dictionary<string, AnimationType> _animationTypeMap = new Dictionary<string, AnimationType>();

        protected struct TimelineClipInfo
        {
            public string ClipName { get; set; }
            public double Duration { get; set; } // Animation全体の長さ(s)(本来の長さにSpeedを掛けたもの、実際にかかる時間)
            public double BlendInDuration { get; set; }
            public double BlendOutDuration { get; set; }
        }

        // Die Timeline
        protected List<TimelineClipInfo> _dieClipInfoList = new List<TimelineClipInfo>();

        // Combat
        protected class CombatTimelineInfo
        {
            public string CombatName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        protected CombatTimelineInfo _combatTimelineInfo;
        protected CombatTimelineInfo _curCombat;

        // Skill
        protected class SkillTimelineInfo
        {
            public string SkillName { get; set; }
            public List<TimelineClipInfo> TimelineClipInfoList { get; set; } = new List<TimelineClipInfo>();
        }
        protected List<SkillTimelineInfo> _skillTimelineInfos = new List<SkillTimelineInfo>();
        protected SkillTimelineInfo _curSkill;

        protected int _allClipCount;

        protected int _curPlayingIndex = -1, _nextPlayingIndex = -1;
        protected int _disposedPlayingIndex = -1;

        protected float rate = 0f;
        protected float fixedRate = 0f; // 遷移中に割り込みが発生したときに本来の遷移がどの程度だったか

        protected CancellationTokenSource _cancellationTokenSource;

        [Inject]
        public void Construct(
            IProfileSetting profileSetting,
            IDieSetting dieSetting,
            IAnimatorController animatorController,
            ICombatSpawnObservable combatSpawnObservable,
            ISkillSpawnObservable skillSpawnObservable
        )
        {
            _profileSetting = profileSetting;
            _dieSetting = dieSetting;
            _animatorController = animatorController;
            _combatSpawnObservable = combatSpawnObservable;
            _skillSpawnObservable = skillSpawnObservable;
        }

        protected float GetFadeLength(string curClip, string nextClip)
        {
            var fadeLength = 0f;
            if (_animationTypeMap.TryGetValue(curClip, out AnimationType curAnimationType) == false ||
                _animationTypeMap.TryGetValue(nextClip, out AnimationType nextAnimationType) == false) return _durationDataContainer.defaultDuration;

            switch (nextAnimationType)
            {
                case AnimationType.Idle:
                    switch (curAnimationType)
                    {
                        case AnimationType.Move: // Move○○ -> Idle
                        case AnimationType.Rotate: // Rotate○○ -> Idle
                        case AnimationType.Damaged: // Damaged -> Idle
                        case AnimationType.Combat: // Combat -> Idle (中断のみ)
                            fadeLength = _durationDataContainer.defaultBackToIdleDuration;
                            var backToIdleDurationData = _durationDataContainer.backToIdleDurations.FirstOrDefault(data => data.clip == curClip);
                            if (backToIdleDurationData != null) fadeLength = backToIdleDurationData.duration;
                            break;
                        default: fadeLength = _durationDataContainer.defaultDuration; break;
                    }
                    break;
                case AnimationType.Move:
                    switch (curAnimationType)
                    {
                        case AnimationType.Idle:
                        case AnimationType.Move:
                            // Idle, Move○○ -> Move○○
                            fadeLength = _durationDataContainer.defaultLocomotionDuration;
                            var locomotionDurationData = _durationDataContainer.locomotionDurations.FirstOrDefault(data => data.clipBefore == curClip && data.clipAfter == nextClip);
                            if (locomotionDurationData != null) fadeLength = locomotionDurationData.duration;
                            break;
                        default: fadeLength = _durationDataContainer.defaultDuration; break;
                    }
                    break;
                case AnimationType.Rotate:
                    switch (curAnimationType)
                    {
                        case AnimationType.Idle:
                        case AnimationType.Move:
                            // Idle, Move○○ -> Rotate○○ 
                            fadeLength = _durationDataContainer.defaultRotateStartDuration;
                            var rotateStartDurationData = _durationDataContainer.rotateStartDurations.FirstOrDefault(data => data.clip == curClip);
                            if (rotateStartDurationData != null) fadeLength = rotateStartDurationData.duration;
                            break;
                        default: fadeLength = _durationDataContainer.defaultDuration; break;

                    }
                    break;
                case AnimationType.Damaged:
                    fadeLength = _durationDataContainer.defaultDamagedDuration;
                    var damagedDurationData = _durationDataContainer.damagedDurations.FirstOrDefault(data => data.clip == curClip);
                    if (damagedDurationData != null) fadeLength = damagedDurationData.duration;
                    break;
                //! DieはTimeline
                default: fadeLength = _durationDataContainer.defaultDuration; break;
            }
            return fadeLength;
        }

        protected void Play(string nextClip)
        {
            // 最初の遷移
            if (_curPlayingIndex < 0)
            {
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

                _mixer.SetInputWeight(_curPlayingIndex, 1);

                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);

                return;
            }

            if (_cancellationTokenSource == null) PlayWithoutInterrupt(nextClip);
            else PlayWithInterrupt(nextClip);
        }

        protected virtual void PlayWithoutInterrupt(string nextClip) // 割り込みなし
        {
            // Combatですか？
            if (_combatTimelineInfo?.CombatName == nextClip)
            {
                if (_curCombat != null) return;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                return;
            }

            // Skillですか？
            var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                return;
            }

            // Die
            var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out AnimationType type) && type == AnimationType.Die);
            if (isDieClip)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                InternalPlayDie(_cancellationTokenSource.Token).Forget();
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
        }

        protected virtual void PlayWithInterrupt(string nextClip) // 割り込み
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            AnimationType type;

            // Damaged
            if (nextClip == "Damaged")
            {
                TokenCancel();

                if (_curCombat != null) FinishCombat();
                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                return;
            }

            // Die
            var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out type) && type == AnimationType.Die);
            if (isDieClip)
            {
                TokenCancel();

                if (_curCombat != null) FinishCombat();
                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalPlayDie(_cancellationTokenSource.Token).Forget();
                return;
            }
        }

        protected async UniTask InternalPlayCombat(CombatTimelineInfo combatTimelineInfo, CancellationToken token)
        {
            _curCombat = combatTimelineInfo;

            for (int i = 0; i < _curCombat.TimelineClipInfoList.Count; i++)
            {
                var timelineClipInfo = _curCombat.TimelineClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0)
                {
                    fadeLength = _durationDataContainer.defaultCombatStartDuration;
                    var combatStartDurationData = _durationDataContainer.combatStartDurations.FirstOrDefault(data => data.clip == _curCombat.CombatName);
                    if (combatStartDurationData != null) fadeLength = combatStartDurationData.duration;
                }
                else if (timelineClipInfo.BlendInDuration >= 0)
                {
                    fadeLength = (float)timelineClipInfo.BlendInDuration;
                }

                var blendOutDuration = timelineClipInfo.BlendOutDuration;
                if (blendOutDuration < 0) blendOutDuration = 0f;
                var exitTime = (float)(timelineClipInfo.Duration - blendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                await InternalCrossFade(timelineClipInfo.ClipName, fadeLength, token);

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

        protected async UniTask InternalPlaySkill(SkillTimelineInfo skillTimelineInfo, CancellationToken token)
        {
            _curSkill = skillTimelineInfo;

            for (int i = 0; i < _curSkill.TimelineClipInfoList.Count; i++)
            {
                var timelineClipInfo = _curSkill.TimelineClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0)
                {
                    fadeLength = _durationDataContainer.defaultSkillStartDuration;
                    var skillStartDurationData = _durationDataContainer.skillStartDurations.FirstOrDefault(data => data.clip == _curSkill.SkillName);
                    if (skillStartDurationData != null) fadeLength = skillStartDurationData.duration;
                }
                else if (timelineClipInfo.BlendInDuration >= 0)
                {
                    fadeLength = (float)timelineClipInfo.BlendInDuration;
                }

                var blendOutDuration = timelineClipInfo.BlendOutDuration;
                if (blendOutDuration < 0) blendOutDuration = 0f;
                var exitTime = (float)(timelineClipInfo.Duration - blendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                await InternalCrossFade(timelineClipInfo.ClipName, fadeLength, token);

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

        protected async UniTask InternalPlayDie(CancellationToken token)
        {
            for (int i = 0; i < _dieClipInfoList.Count; i++)
            {
                var timelineClipInfo = _dieClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0) fadeLength = _durationDataContainer.dieStartDuration;
                else if (timelineClipInfo.BlendInDuration >= 0) fadeLength = (float)timelineClipInfo.BlendInDuration;

                var blendOutDuration = timelineClipInfo.BlendOutDuration;
                if (blendOutDuration < 0) blendOutDuration = 0f;
                var exitTime = (float)(timelineClipInfo.Duration - blendOutDuration);
                float exitWaitTime = Time.timeSinceLevelLoad + exitTime;

                await InternalCrossFade(timelineClipInfo.ClipName, fadeLength, token);

                await UniTask.WaitWhile(() =>
                {
                    var diff = exitWaitTime - Time.timeSinceLevelLoad;
                    return (diff > 0);
                }, cancellationToken: token);
            }

            // 割り込みがなかった場合のみここまで辿り着く
            TokenCancel();

            //TODO: FinishDie(一定時間後にDestroy)->Dieは割り込まれることがないから現状(DieTimelineがstop時にFinish処理)のままで良いのでは
        }

        protected async UniTask InternalAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            var isCurDamagedClip = _playables[_curPlayingIndex].GetAnimationClip().name == "Damaged";

            await InternalCrossFade(nextClip, transitionTime, token);

            // 割り込みがなかった場合のみここまで辿り着く
            if (isCurDamagedClip && nextClip == "Idle") _onFinishDamaged.OnNext(Unit.Default);
            TokenCancel();
        }

        protected async UniTask InternalCrossFade(string nextClip, float transitionTime, CancellationToken token)
        {
            _disposedPlayingIndex = _nextPlayingIndex;

            //! _curPlayingIndex || _nextPlayingIndexで割り込む場合
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip || 
                (_nextPlayingIndex != -1 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip))
            {
                // curPlayingIndex || nextPlayingIndexと同じClipのPlayableを作成しmixerに繋げる
                _playables.Add(AnimationClipPlayable.Create(_graph, _clips.FirstOrDefault(clip => clip.name == nextClip)));
                _mixer.AddInput(_playables[_playables.Count - 1], 0, 0);

                _nextPlayingIndex = _playables.Count - 1;
            }
            else
            {
                _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);
            }

            // 次に再生するクリップは最初(time = 0)から再生(こうしないとLoopOffのClipへ遷移するときにtimeが大きすぎて再生されない場合がある)
            _playables[_nextPlayingIndex].SetTime(0);
            _mixer.GetInput(_nextPlayingIndex).SetTime(0);

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
                    if(_disposedPlayingIndex >= 0)
                    {
                        _mixer.SetInputWeight(_disposedPlayingIndex, 0);
                        _disposedPlayingIndex = -1;
                    }

                    _mixer.SetInputWeight(_nextPlayingIndex, 0);
                    _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == _playables[_nextPlayingIndex].GetAnimationClip().name);
                    _mixer.SetInputWeight(_curPlayingIndex, 1);

                    if (_nextPlayingIndex >= _allClipCount)
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
                    if (_disposedPlayingIndex >= 0)
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

            // 増やしたplayableを消す
            _mixer.SetInputCount(_allClipCount);
            if (_playables.Count > _allClipCount) _playables.RemoveRange(_allClipCount, _playables.Count - _allClipCount);
        }

        protected void TokenCancel()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        protected void FinishCombat()
        {
            _curCombat = null;
            _onFinishCombat.OnNext(Unit.Default);
        }

        protected void FinishSkill()
        {
            _curSkill = null;
            _onFinishSkill.OnNext(Unit.Default);
        }

        protected void InternalInit()
        {
            SetupGraph();

            _animationTypeMap.Add("Idle", AnimationType.Idle);

            Array.ForEach(AnimationExtensions.MoveClips, clipName => _animationTypeMap.Add(clipName, AnimationType.Move));

            Array.ForEach(AnimationExtensions.RotateClips, clipName => _animationTypeMap.Add(clipName, AnimationType.Rotate));
            //TODO: 仮
            var playerRotateSpeed = 0.5f;
            int index = _playables.FindIndex(x => x.GetAnimationClip().name == "RotateRight");
            if (index >= 0) _playables[index].SetSpeed(playerRotateSpeed);
            index = _playables.FindIndex(x => x.GetAnimationClip().name == "RotateLeft");
            if (index >= 0) _playables[index].SetSpeed(playerRotateSpeed);

            _animationTypeMap.Add("Damaged", AnimationType.Damaged);

            SetupDieAnimation();

            if(_combatSpawnObservable.Combat != null) SetupCombatAnimation(_combatSpawnObservable.Combat.Combat.PlayableAsset);
            Array.ForEach(_skillSpawnObservable.SkillList, skill => SetupSkillAnimation(skill.Skill.PlayableAsset));

            _allClipCount = _playables.Count;
        }

        void SetupGraph()
        {
            _graph = PlayableGraph.Create(_profileSetting.Name + " Playable Graph");
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

        protected void SetupDieAnimation()
        {
            foreach (var trackAsset in (_dieSetting.Timeline as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AnimationTrack)
                {
                    List<TimelineClip> clips = new List<TimelineClip>();
                    foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                    foreach (var clip in clips.OrderBy(clip => clip.start))
                    {
                        var clipName = clip.animationClip.name;

                        _dieClipInfoList.Add(new TimelineClipInfo
                        {
                            ClipName = clipName,
                            Duration = clip.duration,
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Die);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            playable.SetSpeed(clip.timeScale);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0);
                        }
                    }

                    trackAsset.muted = true;
                }
            }
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
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Combat);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            playable.SetSpeed(clip.timeScale);
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

        protected void SetupSkillAnimation(PlayableAsset playableAsset)
        {
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
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Skill);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            playable.SetSpeed(clip.timeScale);
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

        public void FadeToIdle()
        {
            (this as IAnimationController).Play("Idle");
        }

        void OnDestroy()
        {
            TokenCancel();
            _graph.Destroy();
        }
    }
}
