using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

namespace HexRPG.Battle.Player.Member
{
    public class MemberAnimationBehaviour : AnimationBehaviour, IAnimationController
    {
        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;

        IObservable<Unit> IAnimationController.OnFinishCombat => _onFinishCombat;
        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;

        void IAnimationController.Init()
        {
            var name = _profileSetting.Name;
            _durationDataContainer = Resources.Load<DurationDataContainer>
                ("HexRPG/Battle/ScriptableObject/Player/" + name + "/" + name + "DurationDataContainer");

            InternalInit();
        }

        #region AnimationPlayer

        void IAnimationController.Play(string clip)
        {
            Play(clip);
        }

        protected override void PlayWithoutInterrupt(string nextClip)
        {
            //! Damagedの場合のみ自分自身(Damaged)に遷移できる
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            if (_playables[_curPlayingIndex].GetAnimationClip().name == "Damaged" && nextClip == "Damaged")
            {
                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                return;
            }

            //! 遷移中などでない場合、(Damaged -> Damaged)以外は自分自身には遷移しない
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

            base.PlayWithoutInterrupt(nextClip);
        }

        protected override void PlayWithInterrupt(string nextClip)
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            AnimationType type;

            //! Damagedへ遷移中(_nextPlayingIndex = Damaged)のみ_nextPlayingIndex(Damaged)で割り込み可能
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == "Damaged" && nextClip == "Damaged")
            {
                TokenCancel();

                if (_curCombat != null) FinishCombat();
                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                return;
            }

            //! _nextPlayingIndexへ遷移中、_nextPlayingIndexで割り込みしない
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

            // Locomotion->Locomotion遷移中は「Idle, Combat, Skill」割り込み可能
            var isCrossFadeBtwLocomotion =
                (_animationTypeMap.TryGetValue(_playables[_curPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType()) &&
                (_nextPlayingIndex >= 0 && _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType());
            if (isCrossFadeBtwLocomotion)
            {
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

                if (nextClip == "Idle")
                {
                    // 割り込み
                    TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                    return;
                }
            }

            //! Idle -> Rotate遷移中はIdle割り込み可能
            if (curClip == "Idle" && nextClip == "Idle" && _nextPlayingIndex >= 0 &&
                _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsRotateType())
            {
                // 割り込み
                TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // 待ち合わせる必要なし
                return;
            }

            // Combat中断
            var isCombatSuspended = (_curCombat != null && nextClip == "Idle");
            if (isCombatSuspended)
            {
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokenSource.Token.Register(() => FinishCombat());
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                return;
            }

            base.PlayWithInterrupt(nextClip);
        }

        #endregion

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
