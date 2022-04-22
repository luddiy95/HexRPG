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
            public double Duration { get; set; } // Animation�S�̂̒���(s)(�{���̒�����Speed���|�������́A���ۂɂ����鎞��)
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
        float fixedRate = 0f; // �J�ڒ��Ɋ��荞�݂����������Ƃ��ɖ{���̑J�ڂ��ǂ̒��x��������

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

        //TODO: ������duration�Ɉ�����n���̂�editor�m�F�̏ꍇ(runtime�ł�duration�͑S��durationData�������Ă���悤�ɂ�����)
        void IAnimationController.Play(string clip)
        {
            // �ŏ��̑J��
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
                                case AnimationType.Locomotion: // Move���� -> Idle
                                case AnimationType.Damaged: // Damaged -> Idle
                                case AnimationType.Combat: // Combat -> Idle (���f�̂�)
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
                                    // Idle, Move���� -> Move����
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
                // �J�ڒ��ȂǂłȂ��ꍇ�A�������g�ɂ͑J�ڂ��Ȃ�
                if (_playables[_curPlayingIndex].GetAnimationClip().name == clip) return;

                // Combat�ł����H
                if (_combatTimelineInfo.CombatName == clip)
                {
                    if (_curCombat != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                    return;
                }

                // Skill�ł����H
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == clip);
                if (skillTimelineInfo != null)
                {
                    if (_curSkill != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                if (curClip == "Damaged" && clip == "Idle") _cancellationTokenSource.Token.Register(() => _onFinishDamaged.OnNext(Unit.Default));
                InternalAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget();
            }
            else
            {
                //! ���荞��(�񓯊����\�b�h���s�� == CrossFade(�A�j���[�V�����J�ڒ�), Combat/Skill�҂����킹��)

                // _nextPlayingIndex�֑J�ڒ��A_nextPlayingIndex�Ŋ��荞�݂��Ȃ�
                if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == clip) return;

                // Locomotion->Locomotion�J�ڒ��́uIdle, Combat, Skill�v���荞�݉\
                var isCrossFadeBtwLocomotion =
                    (_animationTypeMap.TryGetValue(_playables[_curPlayingIndex].GetAnimationClip().name, out AnimationType type) && type == AnimationType.Locomotion) &&
                    (_animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type == AnimationType.Locomotion);
                if (isCrossFadeBtwLocomotion)
                {
                    // Combat�ł����H
                    if (_combatTimelineInfo.CombatName == clip)
                    {
                        if (_curCombat != null) return;

                        // ���荞��
                        TokenCancel();
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalPlayCombat(_combatTimelineInfo, _cancellationTokenSource.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                        return;
                    }

                    // Skill�ł����H
                    var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == clip);
                    if (skillTimelineInfo != null)
                    {
                        if (_curSkill != null) return;

                        // ���荞��
                        TokenCancel();
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                        return;
                    }

                    if (clip == "Idle")
                    {
                        // ���荞��
                        TokenCancel(); // Token�L�����Z��������await�㑱�����͑S�ČĂ΂�Ȃ�
                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cancellationTokenSource = new CancellationTokenSource();
                        InternalAnimationTransit(clip, GetFadeLength(), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
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

                // Combat���f
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

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
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

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
            TokenCancel();

            FinishSkill();
        }

        async UniTask InternalAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            await InternalCrossFade(nextClip, transitionTime, token);

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
            TokenCancel();
        }

        async UniTask InternalCrossFade(string nextClip, float transitionTime, CancellationToken token, double speed = 1)
        {
            _disposedPlayingIndex = _nextPlayingIndex;
            _nextPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

            //! _curPlayingIndex�Ŋ��荞�ޏꍇ
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip)
            {
                var inputCount = _mixer.GetInputCount();
                if (inputCount > _allClipCount) for (int i = inputCount - 1; i >= _allClipCount; i--) _mixer.SetInputCount(_allClipCount);
                if (_playables.Count > _allClipCount) _playables.RemoveRange(_allClipCount, _playables.Count - _allClipCount);

                // curPlayingIndex�Ɠ���Clip��Playable���쐬��mixer�Ɍq����
                _playables.Add(AnimationClipPlayable.Create(_graph, _clips[_curPlayingIndex]));
                _mixer.AddInput(_playables[_allClipCount], 0, 0);

                _nextPlayingIndex = _allClipCount;
            }

            // ���ɍĐ�����N���b�v�͍ŏ�(time = 0)����Đ�(�������Ȃ���LoopOff��Clip�֑J�ڂ���Ƃ���time���傫�����čĐ�����Ȃ��ꍇ������)
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
            //TODO: ���̕ӂ�Combat�̕��Ŏ��s����ׂ�
            /*
            _skillDirector.stopped += (obj) =>
            {
                //TODO: Stop���ł͂Ȃ��Đ����̃G�t�F�N�g���I������܂ő�����H
                //TODO: Stop����disable�ɂ���
            };
            */

            foreach (var trackAsset in (playableAsset as TimelineAsset).GetOutputTracks())
            {
                if (trackAsset is AnimationTrack)
                {
                    List<TimelineClip> clips = new List<TimelineClip>();
                    foreach (var clip in trackAsset.GetClips()) clips.Add(clip);

                    // skillTimelineInfoList�ɒǉ�
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
            //TODO: Damaged�I���ʒm
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
