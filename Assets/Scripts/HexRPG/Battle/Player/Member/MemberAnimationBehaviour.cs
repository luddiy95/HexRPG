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
            //! Damaged�̏ꍇ�̂ݎ������g(Damaged)�ɑJ�ڂł���
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            if (GetAnimationType(curClip) == AnimationType.Damaged && GetAnimationType(playClip) == AnimationType.Damaged)
            {
                _cts = new CancellationTokenSource();
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget();
                return;
            }

            //! �J�ڒ��ȂǂłȂ��ꍇ�A(Damaged -> Damaged)�ȊO�͎������g�ɂ͑J�ڂ��Ȃ�
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

            //! Damaged�֑J�ڒ�(_nextPlayingIndex = Damaged)�̂�_nextPlayingIndex(Damaged)�Ŋ��荞�݉\
            //! _cts != null�ł�_nextPlayingIndex < 0 �̏ꍇ������(Combat, Skill�Ȃ�)
            if (nextAnimationType == AnimationType.Damaged && GetAnimationType(playClip) == AnimationType.Damaged)
            {
                TokenCancel();

                if (_curCombat != null) FinishCombat();
                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // �҂����킹��K�v�Ȃ�
                return;
            }

            //! _nextPlayingIndex�֑J�ڒ��A_nextPlayingIndex�Ŋ��荞�݂��Ȃ�
            if (nextClip == playClip) return;

            // Idle -> Locomotion, Locomotion -> Idle, Locomotion -> Locomotion �J�ڒ��́uIdle, Combat, Skill�v���荞�݉\
            var isCrossFadeBtwLocomotion =
                (curAnimationType == AnimationType.Idle || curAnimationType == AnimationType.Move) &&
                    (nextAnimationType == AnimationType.Idle || nextAnimationType == AnimationType.Move);
            if (isCrossFadeBtwLocomotion)
            {
                // Combat�ł����H
                if (_combatTimelineInfo?.CombatName == playClip)
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
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == playClip);
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

                if (GetAnimationType(playClip) == AnimationType.Idle)
                {
                    // ���荞��
                    TokenCancel(); // Token�L�����Z��������await�㑱�����͑S�ČĂ΂�Ȃ�
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cts = new CancellationTokenSource();
                    InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // �҂����킹��K�v�Ȃ�
                    return;
                }
            }

            // Locomotion -> Idle �J�ڒ���Locomotion���荞�݉\ (Locomotion -> Locomotion��Locomotion���荞�݂͕s�\)
            var isCrossFadeBtwLocomotionIdle = (curAnimationType == AnimationType.Move && nextAnimationType == AnimationType.Idle);
            if (isCrossFadeBtwLocomotionIdle)
            {
                if (GetAnimationType(playClip) == AnimationType.Move)
                {
                    // ���荞��
                    TokenCancel(); // Token�L�����Z��������await�㑱�����͑S�ČĂ΂�Ȃ�
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cts = new CancellationTokenSource();
                    InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // �҂����킹��K�v�Ȃ�
                    return;
                }
            }

            //! Idle -> Rotate�J�ڒ���Idle���荞�݉\
            if (curAnimationType == AnimationType.Idle && nextAnimationType == AnimationType.Rotate && GetAnimationType(playClip) == AnimationType.Idle)
            {
                // ���荞��
                TokenCancel(); // Token�L�����Z��������await�㑱�����͑S�ČĂ΂�Ȃ�
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cts = new CancellationTokenSource();
                InternalAnimationTransit(playClip, GetFadeLength(curClip, playClip), _cts.Token).Forget(); // �҂����킹��K�v�Ȃ�
                return;
            }

            // Combat���f
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
