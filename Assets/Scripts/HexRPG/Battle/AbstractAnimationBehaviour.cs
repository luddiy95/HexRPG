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
    public abstract class AbstractAnimationBehaviour : MonoBehaviour
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
            public double Duration { get; set; } // AnimationëSëÃÇÃí∑Ç≥(s)(ñ{óàÇÃí∑Ç≥Ç…SpeedÇä|ÇØÇΩÇ‡ÇÃÅAé¿ç€Ç…Ç©Ç©ÇÈéûä‘)
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
        protected float fixedRate = 0f; // ëJà⁄íÜÇ…äÑÇËçûÇ›Ç™î≠ê∂ÇµÇΩÇ∆Ç´Ç…ñ{óàÇÃëJà⁄Ç™Ç«ÇÃíˆìxÇæÇ¡ÇΩÇ©

        protected CancellationTokenSource _cts = null;

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
                        case AnimationType.Move: // MoveÅõÅõ -> Idle
                        case AnimationType.Rotate: // RotateÅõÅõ -> Idle
                        case AnimationType.Damaged: // Damaged -> Idle
                        case AnimationType.Combat: // Combat -> Idle (íÜífÇÃÇ›)
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
                            // Idle, MoveÅõÅõ -> MoveÅõÅõ
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
                            // Idle, MoveÅõÅõ -> RotateÅõÅõ 
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
                //! DieÇÕTimeline
                default: fadeLength = _durationDataContainer.defaultDuration; break;
            }
            return fadeLength;
        }

        protected void Play(string nextClip)
        {
            // ç≈èâÇÃëJà⁄
            if (_curPlayingIndex < 0)
            {
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

                _mixer.SetInputWeight(_curPlayingIndex, 1);

                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);

                return;
            }

            if (_cts == null) PlayWithoutInterrupt(nextClip);
            else PlayWithInterrupt(nextClip);
        }

        protected virtual void PlayWithoutInterrupt(string nextClip) // äÑÇËçûÇ›Ç»Çµ
        {
            // CombatÇ≈Ç∑Ç©ÅH
            if (_combatTimelineInfo?.CombatName == nextClip)
            {
                if (_curCombat != null) return;

                _cts = new CancellationTokenSource();
                InternalPlayCombat(_combatTimelineInfo, _cts.Token).Forget(); // ë“ÇøçáÇÌÇπÇ∑ÇÈïKóvÇÕÇ»Ç¢
                return;
            }

            // SkillÇ≈Ç∑Ç©ÅH
            var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                _cts = new CancellationTokenSource();
                InternalPlaySkill(skillTimelineInfo, _cts.Token).Forget(); // ë“ÇøçáÇÌÇπÇ∑ÇÈïKóvÇÕÇ»Ç¢
                return;
            }

            // Die
            var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out AnimationType type) && type == AnimationType.Die);
            if (isDieClip)
            {
                _cts = new CancellationTokenSource();
                InternalPlayDie(_cts.Token).Forget();
                return;
            }

            _cts = new CancellationTokenSource();
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cts.Token).Forget();
        }

        protected virtual void PlayWithInterrupt(string nextClip) // äÑÇËçûÇ›
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

                _cts = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cts.Token).Forget();
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

                _cts = new CancellationTokenSource();
                InternalPlayDie(_cts.Token).Forget();
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

            // äÑÇËçûÇ›Ç™Ç»Ç©Ç¡ÇΩèÍçáÇÃÇ›Ç±Ç±Ç‹Ç≈íHÇËíÖÇ≠
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

            // äÑÇËçûÇ›Ç™Ç»Ç©Ç¡ÇΩèÍçáÇÃÇ›Ç±Ç±Ç‹Ç≈íHÇËíÖÇ≠
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

            // äÑÇËçûÇ›Ç™Ç»Ç©Ç¡ÇΩèÍçáÇÃÇ›Ç±Ç±Ç‹Ç≈íHÇËíÖÇ≠
            TokenCancel();

            //TODO: FinishDie(àÍíËéûä‘å„Ç…Destroy)->DieÇÕäÑÇËçûÇ‹ÇÍÇÈÇ±Ç∆Ç™Ç»Ç¢Ç©ÇÁåªèÛ(DieTimelineÇ™stopéûÇ…Finishèàóù)ÇÃÇ‹Ç‹Ç≈ó«Ç¢ÇÃÇ≈ÇÕ
        }

        protected async UniTask InternalAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            var isCurDamagedClip = _playables[_curPlayingIndex].GetAnimationClip().name == "Damaged";

            await InternalCrossFade(nextClip, transitionTime, token);

            // äÑÇËçûÇ›Ç™Ç»Ç©Ç¡ÇΩèÍçáÇÃÇ›Ç±Ç±Ç‹Ç≈íHÇËíÖÇ≠
            if (isCurDamagedClip && nextClip == "Idle") _onFinishDamaged.OnNext(Unit.Default);
            TokenCancel();
        }

        protected async UniTask InternalCrossFade(string nextClip, float transitionTime, CancellationToken token)
        {
            _disposedPlayingIndex = _nextPlayingIndex;

            //! _curPlayingIndex || _nextPlayingIndexÇ≈äÑÇËçûÇﬁèÍçá
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip || 
                (_nextPlayingIndex != -1 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip))
            {
                // curPlayingIndex || nextPlayingIndexÇ∆ìØÇ∂ClipÇÃPlayableÇçÏê¨ÇµmixerÇ…åqÇ∞ÇÈ
                _playables.Add(AnimationClipPlayable.Create(_graph, _clips.FirstOrDefault(clip => clip.name == nextClip)));
                _mixer.AddInput(_playables[_playables.Count - 1], 0, 0);

                _nextPlayingIndex = _playables.Count - 1;
            }
            else
            {
                _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);
            }

            // éüÇ…çƒê∂Ç∑ÇÈÉNÉäÉbÉvÇÕç≈èâ(time = 0)Ç©ÇÁçƒê∂(Ç±Ç§ÇµÇ»Ç¢Ç∆LoopOffÇÃClipÇ÷ëJà⁄Ç∑ÇÈÇ∆Ç´Ç…timeÇ™ëÂÇ´Ç∑Ç¨Çƒçƒê∂Ç≥ÇÍÇ»Ç¢èÍçáÇ™Ç†ÇÈ)
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

            // ëùÇ‚ÇµÇΩplayableÇè¡Ç∑
            _mixer.SetInputCount(_allClipCount);
            if (_playables.Count > _allClipCount) _playables.RemoveRange(_allClipCount, _playables.Count - _allClipCount);
        }

        protected void TokenCancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
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
            //TODO: âº
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

                    // combatTimelineInfoListÇ…í«â¡
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

                    // skillTimelineInfoListÇ…í«â¡
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

        #region Inspector

        public abstract void SetupDamaged();

        public virtual void OnInspectorGUI()
        {
            if (GUILayout.Button("SetupDamaged"))
            {
                SetupDamaged();
            }
        }

        #endregion
    }
}
