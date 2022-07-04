using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UniRx;
using UnityEditor;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    public class EnemyAnimationBehaviour : AbstractAnimationBehaviour, IAnimationController
    {
        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;

        IObservable<Unit> IAnimationController.OnFinishCombat => _onFinishCombat;
        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;

        void IAnimationController.Init()
        {
            if(_curPlayingIndex >= 0)
            {
                _mixer.SetInputWeight(_curPlayingIndex, 0);
                _curPlayingIndex = -1;
                return;
            }

            var name = _profileSetting.Name;
            _durationDataContainer = Resources.Load<DurationDataContainer>
                ("HexRPG/Battle/ScriptableObject/Enemy/" + name + "/" + name + "DurationDataContainer");

            InternalInit();
        }

        void IAnimationController.Play(string nextClip)
        {
            Play(nextClip);
        }

        protected override void PlayWithoutInterrupt(string nextClip)
        {
            //! �J�ڒ��ȂǂłȂ��ꍇ�A�������g�ɂ͑J�ڂ��Ȃ�
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

            base.PlayWithoutInterrupt(nextClip);
        }

        protected override void PlayWithInterrupt(string nextClip)
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;

            //! _nextPlayingIndex�֑J�ڒ��A_nextPlayingIndex�Ŋ��荞�݂��Ȃ�
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

            var isCurClipIdleLocomotion = (curClip == "Idle" || (_animationTypeMap.TryGetValue(curClip, out AnimationType type) && type.IsLocomotionType()));
            if (isCurClipIdleLocomotion)
            {
                // Idle, Locomotion -> Rotate�́uIdle�v�͊��荞�݉\
                var isCrossFadeBtwIdleLocomotionRotate = 
                    (_nextPlayingIndex >= 0 && _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsRotateType());
                if (isCrossFadeBtwIdleLocomotionRotate)
                {
                    if (nextClip == "Idle")
                    {
                        TokenCancel();

                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cts = new CancellationTokenSource();
                        InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cts.Token).Forget();
                        return;
                    }
                }

                // Idle, Locomotion -> Locomotion�́uIdle, Rotate�v�͊��荞�݉\
                var isCrossFadeBtwIdleLocomotion = 
                    (_nextPlayingIndex >= 0 && _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType());
                if (isCrossFadeBtwIdleLocomotion)
                {
                    if (nextClip == "Idle" || _animationTypeMap.TryGetValue(nextClip, out type) && type.IsRotateType())
                    {
                        TokenCancel();

                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cts = new CancellationTokenSource();
                        InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cts.Token).Forget();
                        return;
                    }
                }
            }

            // Combat�ł����H
            if (_combatTimelineInfo?.CombatName == nextClip)
            {
                if (_curCombat != null) return;

                // ���荞��
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalPlayCombat(_combatTimelineInfo, _cts.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                return;
            }

            // Skill�ł����H
            var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                // ���荞��
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalPlaySkill(skillTimelineInfo, _cts.Token).Forget(); // �҂����킹����K�v�͂Ȃ�
                return;
            }

            base.PlayWithInterrupt(nextClip);
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
                    time = damagedClip.length * 0.9f,
                    functionName = "FadeToIdle"
                }
            };
            AnimationUtility.SetAnimationEvents(damagedClip, damagedToIdleEvent);
            _graph.Destroy();
        }

        [CustomEditor(typeof(EnemyAnimationBehaviour))]
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

                ((EnemyAnimationBehaviour)target).OnInspectorGUI();
            }
        }

#endif
    }
}
