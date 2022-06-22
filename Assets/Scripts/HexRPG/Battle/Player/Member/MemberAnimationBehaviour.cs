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
            //! Damaged�̏ꍇ�̂ݎ������g(Damaged)�ɑJ�ڂł���
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            if (_playables[_curPlayingIndex].GetAnimationClip().name == "Damaged" && nextClip == "Damaged")
            {
                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                return;
            }

            //! �J�ڒ��ȂǂłȂ��ꍇ�A(Damaged -> Damaged)�ȊO�͎������g�ɂ͑J�ڂ��Ȃ�
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

            base.PlayWithoutInterrupt(nextClip);
        }

        protected override void PlayWithInterrupt(string nextClip)
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;
            AnimationType type;

            //! Damaged�֑J�ڒ�(_nextPlayingIndex = Damaged)�̂�_nextPlayingIndex(Damaged)�Ŋ��荞�݉\
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == "Damaged" && nextClip == "Damaged")
            {
                TokenCancel();

                if (_curCombat != null) FinishCombat();
                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
                return;
            }

            //! _nextPlayingIndex�֑J�ڒ��A_nextPlayingIndex�Ŋ��荞�݂��Ȃ�
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

            // Locomotion->Locomotion�J�ڒ��́uIdle, Combat, Skill�v���荞�݉\
            var isCrossFadeBtwLocomotion =
                (_animationTypeMap.TryGetValue(_playables[_curPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType()) &&
                (_nextPlayingIndex >= 0 && _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsLocomotionType());
            if (isCrossFadeBtwLocomotion)
            {
                // Combat�ł����H
                if (_combatTimelineInfo?.CombatName == nextClip)
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
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
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

                if (nextClip == "Idle")
                {
                    // ���荞��
                    TokenCancel(); // Token�L�����Z��������await�㑱�����͑S�ČĂ΂�Ȃ�
                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
                    return;
                }
            }

            //! Idle -> Rotate�J�ڒ���Idle���荞�݉\
            if (curClip == "Idle" && nextClip == "Idle" && _nextPlayingIndex >= 0 &&
                _animationTypeMap.TryGetValue(_playables[_nextPlayingIndex].GetAnimationClip().name, out type) && type.IsRotateType())
            {
                // ���荞��
                TokenCancel(); // Token�L�����Z��������await�㑱�����͑S�ČĂ΂�Ȃ�
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget(); // �҂����킹��K�v�Ȃ�
                return;
            }

            // Combat���f
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
