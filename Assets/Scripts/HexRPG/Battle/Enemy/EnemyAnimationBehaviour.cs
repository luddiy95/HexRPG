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
            //! 遷移中などでない場合、自分自身には遷移しない
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

            base.PlayWithoutInterrupt(nextClip);
        }

        protected override void PlayWithInterrupt(string nextClip)
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;

            //! _nextPlayingIndexへ遷移中、_nextPlayingIndexで割り込みしない
            //! _cts != nullでも_nextPlayingIndex < 0 の場合がある(Combat, Skillなど)
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

            var isCurClipIdleLocomotion = (GetAnimationType(curClip) == AnimationType.Idle || GetAnimationType(curClip) == AnimationType.Move);
            if (isCurClipIdleLocomotion)
            {
                // Idle, Locomotion -> Rotateは「Idle」は割り込み可能
                var isCrossFadeBtwIdleLocomotionRotate = 
                    (_nextPlayingIndex >= 0 && GetAnimationType(_playables[_nextPlayingIndex].GetAnimationClip().name) == AnimationType.Rotate);
                if (isCrossFadeBtwIdleLocomotionRotate)
                {
                    if(GetAnimationType(nextClip) == AnimationType.Idle)
                    {
                        TokenCancel();

                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cts = new CancellationTokenSource();
                        InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cts.Token).Forget();
                        return;
                    }
                }

                // Idle, Locomotion -> Locomotionは「Idle, Rotate」は割り込み可能
                var isCrossFadeBtwIdleLocomotion = 
                    (_nextPlayingIndex >= 0 && GetAnimationType(_playables[_nextPlayingIndex].GetAnimationClip().name) == AnimationType.Move);
                if (isCrossFadeBtwIdleLocomotion)
                {
                    if (GetAnimationType(nextClip) == AnimationType.Idle || GetAnimationType(nextClip) == AnimationType.Rotate)
                    {
                        TokenCancel();

                        if (_nextPlayingIndex >= 0) fixedRate = rate;

                        _cts = new CancellationTokenSource();
                        InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cts.Token).Forget();
                        return;
                    }
                }
            }

            // Combatですか？
            if (_combatTimelineInfo?.CombatName == nextClip)
            {
                if (_curCombat != null) return;

                // 割り込み
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalPlayCombat(_combatTimelineInfo, _cts.Token).Forget(); // 待ち合わせする必要はない
                return;
            }

            // Skillですか？
            var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                // 割り込み
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalPlaySkill(skillTimelineInfo, _cts.Token).Forget(); // 待ち合わせする必要はない
                return;
            }

            base.PlayWithInterrupt(nextClip);
        }

#if UNITY_EDITOR

        public override void SetupDamaged()
        {
            _graph = PlayableGraph.Create();
            _playables = _clips.Select(clip => AnimationClipPlayable.Create(_graph, clip)).ToList();
            var damagedClip = _playables.First(playable => GetAnimationType(playable.GetAnimationClip().name) == AnimationType.Damaged).GetAnimationClip();
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
