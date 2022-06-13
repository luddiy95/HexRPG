using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UniRx;
using UnityEditor;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public class EnemyAnimationBehaviour : AnimationBehaviour, IAnimationController
    {
        ISkillSpawnObservable _skillSpawnObservable;

        IObservable<Unit> IAnimationController.OnFinishDamaged => _onFinishDamaged;

        IObservable<Unit> IAnimationController.OnFinishCombat => null;

        IObservable<Unit> IAnimationController.OnFinishSkill => _onFinishSkill;

        [Inject]
        public void Construct(
            IProfileSetting profileSetting,
            IDieSetting dieSetting,
            IAnimatorController animatorController,
            ISkillSpawnObservable skillSpawnObservable
        )
        {
            _profileSetting = profileSetting;
            _dieSetting = dieSetting;
            _animatorController = animatorController;
            _skillSpawnObservable = skillSpawnObservable;
        }

        void IAnimationController.Init()
        {
            var name = _profileSetting.Name;
            _durationDataContainer = Resources.Load<DurationDataContainer>
                ("HexRPG/Battle/ScriptableObject/Enemy/" + name + "/" + name + "DurationDataContainer");

            SetupGraph();

            _animationTypeMap.Add("Idle", AnimationType.Idle);
            Array.ForEach(AnimationExtensions.MoveClips, clipName => _animationTypeMap.Add(clipName, AnimationType.Move));

            _animationTypeMap.Add("RotateRight", AnimationType.Rotate);
            _animationTypeMap.Add("RotateLeft", AnimationType.Rotate);
            //TODO: ‰¼
            var playerRotateSpeed = 0.5f;
            int index = _playables.FindIndex(x => x.GetAnimationClip().name == "RotateRight");
            if (index >= 0) _playables[index].SetSpeed(playerRotateSpeed);
            index = _playables.FindIndex(x => x.GetAnimationClip().name == "RotateLeft");
            if (index >= 0) _playables[index].SetSpeed(playerRotateSpeed);

            _animationTypeMap.Add("Damaged", AnimationType.Damaged);

            SetupDieAnimation();

            Array.ForEach(_skillSpawnObservable.SkillList, skill => SetupSkillAnimation(skill.Skill.PlayableAsset));

            _allClipCount = _playables.Count;
        }

        void IAnimationController.Play(string nextClip)
        {
            Play(nextClip);
        }

        protected override void PlayWithoutInterrupt(string nextClip)
        {
            //! ‘JˆÚ’†‚È‚Ç‚Å‚È‚¢ê‡AŽ©•ªŽ©g‚É‚Í‘JˆÚ‚µ‚È‚¢
            if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

            base.PlayWithoutInterrupt(nextClip);
        }

        protected override void PlayWithInterrupt(string nextClip)
        {
            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;

            //! _nextPlayingIndex‚Ö‘JˆÚ’†A_nextPlayingIndex‚ÅŠ„‚èž‚Ý‚µ‚È‚¢
            if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

            // Skill‚Å‚·‚©H
            var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
            if (skillTimelineInfo != null)
            {
                if (_curSkill != null) return;

                // Š„‚èž‚Ý
                TokenCancel();
                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // ‘Ò‚¿‡‚í‚¹‚·‚é•K—v‚Í‚È‚¢
                return;
            }

            // Damaged
            if (nextClip == "Damaged")
            {
                TokenCancel();

                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
                return;
            }

            // Die
            var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out AnimationType type) && type == AnimationType.Die);
            if (isDieClip)
            {
                TokenCancel();

                if (_curSkill != null) FinishSkill();

                if (_nextPlayingIndex >= 0) fixedRate = rate;

                _cancellationTokenSource = new CancellationTokenSource();
                InternalPlayDie(_cancellationTokenSource.Token).Forget();
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
