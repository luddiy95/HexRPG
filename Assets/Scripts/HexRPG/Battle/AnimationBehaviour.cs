using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Timeline;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace HexRPG.Battle
{
    public class AnimationBehaviour : AbstractAnimationBehaviour
    {
        protected IProfileSetting _profileSetting;
        protected IDieSetting _dieSetting;
        protected IAnimatorController _animatorController;

        [SerializeField] protected DurationData _durationData;

        // Playable
        protected PlayableGraph _graph;
        protected List<AnimationClipPlayable> _playables;
        protected AnimationMixerPlayable _mixer;

        [SerializeField] protected AnimationClip[] _clips;
        protected Dictionary<string, AnimationType> _animationTypeMap = new Dictionary<string, AnimationType>();

        protected struct TimelineClipInfo
        {
            public string ClipName { get; set; }
            public double Duration { get; set; } // Animation�S�̂̒���(s)(�{���̒�����Speed���|�������́A���ۂɂ����鎞��)
            public double Speed { get; set; }
            public double BlendInDuration { get; set; }
            public double BlendOutDuration { get; set; }
        }

        // Die Timeline
        protected List<TimelineClipInfo> _dieClipInfoList = new List<TimelineClipInfo>();

        protected int _allClipCount;

        protected int _curPlayingIndex = -1, _nextPlayingIndex = -1;
        protected int _disposedPlayingIndex = -1;

        protected float rate = 0f;
        protected float fixedRate = 0f; // �J�ڒ��Ɋ��荞�݂����������Ƃ��ɖ{���̑J�ڂ��ǂ̒��x��������

        protected CancellationTokenSource _cancellationTokenSource;

        protected float GetFadeLength(string curClip, string nextClip)
        {
            var fadeLength = 0f;
            if (_animationTypeMap.TryGetValue(curClip, out AnimationType curAnimationType) == false ||
                _animationTypeMap.TryGetValue(nextClip, out AnimationType nextAnimationType) == false) return _durationData.defaultDuration;

            switch (nextAnimationType)
            {
                case AnimationType.Idle:
                    switch (curAnimationType)
                    {
                        case AnimationType.Move: // Move���� -> Idle
                        case AnimationType.Damaged: // Damaged -> Idle
                        case AnimationType.Combat: // Combat -> Idle (���f�̂�)
                            fadeLength = _durationData.defaultBackToIdleDuration;
                            var backToIdleDurationData = _durationData.backToIdleDurations.FirstOrDefault(data => data.clipBefore == curClip);
                            if (backToIdleDurationData != null) fadeLength = backToIdleDurationData.duration;
                            break;
                        default: fadeLength = _durationData.defaultDuration; break;
                    }
                    break;
                case AnimationType.Move:
                    switch (curAnimationType)
                    {
                        case AnimationType.Idle:
                        case AnimationType.Move:
                            // Idle, Move���� -> Move����
                            fadeLength = _durationData.defaultLocomotionDuration;
                            var locomotionDurationData = _durationData.locomotionDurations.FirstOrDefault(data => data.clipBefore == curClip && data.clipAfter == nextClip);
                            if (locomotionDurationData != null) fadeLength = locomotionDurationData.duration;
                            break;
                        default: fadeLength = _durationData.defaultDuration; break;
                    }
                    break;
                case AnimationType.Damaged:
                    fadeLength = _durationData.defaultDamagedDuration;
                    var damagedDurationData = _durationData.damagedDurations.FirstOrDefault(data => data.clipBefore == curClip);
                    if (damagedDurationData != null) fadeLength = damagedDurationData.duration;
                    break;
                //! Die��Timeline
                default: fadeLength = _durationData.defaultDuration; break;
            }
            return fadeLength;
        }


        protected async UniTask InternalPlayDie(CancellationToken token)
        {
            for (int i = 0; i < _dieClipInfoList.Count; i++)
            {
                var timelineClipInfo = _dieClipInfoList[i];

                var fadeLength = 0f;
                if (i == 0) fadeLength = _durationData.dieStartDuration;
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

            //TODO: FinishDie(��莞�Ԍ��Destroy)
        }

        protected async UniTask InternalAnimationTransit(string nextClip, float transitionTime, CancellationToken token)
        {
            await InternalCrossFade(nextClip, transitionTime, token);

            // ���荞�݂��Ȃ������ꍇ�݂̂����܂ŒH�蒅��
            TokenCancel();
        }

        protected async UniTask InternalCrossFade(string nextClip, float transitionTime, CancellationToken token, double speed = 1)
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
        }

        protected void TokenCancel()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        protected void SetupGraph()
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
                            Speed = clip.timeScale,
                            BlendInDuration = clip.blendInDuration,
                            BlendOutDuration = clip.blendOutDuration,
                        });

                        if (!_playables.Any(playable => playable.GetAnimationClip().name == clipName))
                        {
                            _animationTypeMap.Add(clipName, AnimationType.Die);
                            var playable = AnimationClipPlayable.Create(_graph, clip.animationClip);
                            _playables.Add(playable);
                            _mixer.AddInput(playable, 0, 0);
                        }
                    }

                    trackAsset.muted = true;
                }
            }
        }
    }
}