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
    public class EnemyAnimationBehaviour : AnimationBehaviour, IAnimationController
    {
        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;

        IObservable<Unit> IAnimationController.OnFinishCombat => _onFinishCombat;
        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;

        void IAnimationController.Init()
        {
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
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

            //! Enemyは「Idle, Rotate」は割り込み可能
            if (nextClip == "Idle" || (_animationTypeMap.TryGetValue(nextClip, out AnimationType type) && type.IsRotateType()))
            {
                TokenCancel();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                return;
            }

            // Combatですか？
            if (_combatTimelineInfo?.CombatName == nextClip)
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
            var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
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
