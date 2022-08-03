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
    public class MemberAnimationBehaviour : AbstractAnimationBehaviour, IAnimationController
    {
        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;

        IObservable<Unit> IAnimationController.OnFinishCombat => _onFinishCombat;
        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;

        void IAnimationController.Init()
        {
            if (_curPlayingIndex >= 0)
            {
                _mixer.SetInputWeight(_curPlayingIndex, 0);
                _curPlayingIndex = -1;
                return;
            }

            var name = _profileSetting.Name;
            _durationDataContainer = Resources.Load<DurationDataContainer>
                ("HexRPG/Battle/ScriptableObject/Player/" + name + "/" + name + "DurationDataContainer");

            InternalInit();
        }

        #region AnimationPlayer

        void IAnimationController.Play(string playClip)
        {
            Play(playClip);
        }

        protected override void PlayWithoutInterrupt(string playClip)
        {
            //! Damagedの場合のみ自分自身(Damaged)に遷移できる
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            if (GetAnimationType(curClip) == AnimationType.Damaged && GetAnimationType(playClip) == AnimationType.Damaged)
            {
                _cts = new CancellationTokenSource();
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget();
                return;
            }

            //! 遷移中などでない場合、(Damaged -> Damaged)以外は自分自身には遷移しない
            if (curClip == playClip) return;

            base.PlayWithoutInterrupt(playClip);
        }

        protected override void PlayWithInterrupt(string playClip)
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            var curAnimationType = GetAnimationType(curClip);

            string nextClip = "";
            if (_nextPlayingIndex >= 0) nextClip = _playables[_nextPlayingIndex].GetAnimationClip().name;
            var nextAnimationType = GetAnimationType(nextClip);

            //! Damagedへ遷移中(_nextPlayingIndex = Damaged)のみ_nextPlayingIndex(Damaged)で割り込み可能
            //! _cts != nullでも_nextPlayingIndex < 0 の場合がある(Combat, Skillなど)
            if (nextAnimationType == AnimationType.Damaged && GetAnimationType(playClip) == AnimationType.Damaged)
            {
                TokenCancel();

                if (_curCombat != null) FinishCombat();
                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // 待ち合わせる必要なし
                return;
            }

            //! _nextPlayingIndexへ遷移中、_nextPlayingIndexで割り込みしない
            if (nextClip == playClip) return;

            // Idle -> Locomotion, Locomotion -> Idle, Locomotion -> Locomotion 遷移中は「Idle, Combat, Skill」割り込み可能
            var isCrossFadeBtwLocomotion =
                (curAnimationType == AnimationType.Idle || curAnimationType == AnimationType.Move) &&
                    (nextAnimationType == AnimationType.Idle || nextAnimationType == AnimationType.Move);
            if (isCrossFadeBtwLocomotion)
            {
                // Combatですか？
                if (_combatTimelineInfo?.CombatName == playClip)
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
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == playClip);
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

                if (GetAnimationType(playClip) == AnimationType.Idle)
                {
                    // 割り込み
                    TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cts = new CancellationTokenSource();
                    InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // 待ち合わせる必要なし
                    return;
                }
            }

            // Locomotion -> Idle 遷移中はLocomotion割り込み可能 (Locomotion -> LocomotionにLocomotion割り込みは不可能)
            var isCrossFadeBtwLocomotionIdle = (curAnimationType == AnimationType.Move && nextAnimationType == AnimationType.Idle);
            if (isCrossFadeBtwLocomotionIdle)
            {
                if (GetAnimationType(playClip) == AnimationType.Move)
                {
                    // 割り込み
                    TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cts = new CancellationTokenSource();
                    InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // 待ち合わせる必要なし
                    return;
                }
            }

            //! Idle -> Rotate遷移中はIdle割り込み可能
            if (curAnimationType == AnimationType.Idle && nextAnimationType == AnimationType.Rotate && GetAnimationType(playClip) == AnimationType.Idle)
            {
                // 割り込み
                TokenCancel(); // Tokenキャンセルしたらawait後続処理は全て呼ばれない
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // 待ち合わせる必要なし
                return;
            }

            // Combat中断
            var isCombatSuspended = (_curCombat != null && GetAnimationType(playClip) == AnimationType.Idle);
            if (isCombatSuspended)
            {
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                _cts.Token.Register(() => FinishCombat());
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget();
                return;
            }

            base.PlayWithInterrupt(playClip);
        }

        #endregion

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

        [CustomEditor(typeof(MemberAnimationBehaviour))]
        public class CustomInspector : Editor
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
