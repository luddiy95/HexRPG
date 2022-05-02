using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UniRx;
using UnityEngine;
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
        readonly ISubject<Unit> _onFinishDamaged = new Subject<Unit>();

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
            SetupGraph();

            //TODO: 仮
            _animationTypeMap.Add(_playables[0].GetAnimationClip().name, AnimationType.Idle);
            _animationTypeMap.Add(_playables[1].GetAnimationClip().name, AnimationType.Die);

            SetupDieAnimation();

            Array.ForEach(_skillSpawnObservable.SkillList, skill => SetupSkillAnimation(skill.Skill.PlayableAsset));

            _allClipCount = _playables.Count;
        }

        void IAnimationController.Play(string nextClip)
        {
            // 最初の遷移
            if (_curPlayingIndex < 0)
            {
                _curPlayingIndex = _playables.FindIndex(x => x.GetAnimationClip().name == nextClip);

                _mixer.SetInputWeight(_curPlayingIndex, 1);

                _mixer.SetTime(0);
                _playables[_curPlayingIndex].SetTime(0);

                return;
            }

            var curClip = _playables[_curPlayingIndex].GetAnimationClip().name;

            if (_cancellationTokenSource == null)
            {
                // 遷移中などでない場合、自分自身には遷移しない
                if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

                // Skillですか？
                var skillTimelineInfo = _skillTimelineInfos.FirstOrDefault(info => info.SkillName == nextClip);
                if (skillTimelineInfo != null)
                {
                    if (_curSkill != null) return;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlaySkill(skillTimelineInfo, _cancellationTokenSource.Token).Forget(); // 待ち合わせする必要はない
                    return;
                }

                // Die
                var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out AnimationType type) && type == AnimationType.Die);
                if (isDieClip)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayDie(_cancellationTokenSource.Token).Forget();
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                if (curClip == "Damaged" && nextClip == "Idle") _cancellationTokenSource.Token.Register(() => _onFinishDamaged.OnNext(Unit.Default));
                InternalAnimationTransit(nextClip, GetFadeLength(curClip, nextClip), _cancellationTokenSource.Token).Forget();
            }
            else
            {
                //! 割り込み(非同期メソッド実行中 == CrossFade(アニメーション遷移中), Combat/Skill待ち合わせ中)

                // _nextPlayingIndexへ遷移中、_nextPlayingIndexで割り込みしない
                if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

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

                // Die
                var isDieClip = (_animationTypeMap.TryGetValue(nextClip, out AnimationType type) && type == AnimationType.Die);
                if (isDieClip)
                {
                    TokenCancel();

                    if (_nextPlayingIndex >= 0) fixedRate = rate;

                    _cancellationTokenSource = new CancellationTokenSource();
                    InternalPlayDie(_cancellationTokenSource.Token).Forget();
                    return;
                }
            }
        }
    }
}
