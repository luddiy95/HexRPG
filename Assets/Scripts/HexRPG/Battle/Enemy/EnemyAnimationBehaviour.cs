using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UniRx;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public class EnemyAnimationBehaviour : AnimationBehaviour, IAnimationController
    {
        public IObservable<Unit> OnFinishDamaged => _onFinishDamaged;
        readonly ISubject<Unit> _onFinishDamaged = new Subject<Unit>();

        //TODO: ��
        public IObservable<Unit> OnFinishCombat => null;
        public IObservable<Unit> OnFinishSkill => null;

        [Inject]
        public void Construct(
            IProfileSetting profileSetting,
            IDieSetting dieSetting,
            IAnimatorController animatorController
        )
        {
            _profileSetting = profileSetting;
            _dieSetting = dieSetting;
            _animatorController = animatorController;
        }

        void IAnimationController.Init()
        {
            SetupGraph();

            //TODO: ��
            _animationTypeMap.Add(_playables[0].GetAnimationClip().name, AnimationType.Idle);
            _animationTypeMap.Add(_playables[1].GetAnimationClip().name, AnimationType.Die);

            SetupDieAnimation();

            _allClipCount = _playables.Count;
        }

        void IAnimationController.Play(string nextClip)
        {
            // �ŏ��̑J��
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
                // �J�ڒ��ȂǂłȂ��ꍇ�A�������g�ɂ͑J�ڂ��Ȃ�
                if (_playables[_curPlayingIndex].GetAnimationClip().name == nextClip) return;

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
                //! ���荞��(�񓯊����\�b�h���s�� == CrossFade(�A�j���[�V�����J�ڒ�), Combat/Skill�҂����킹��)

                // _nextPlayingIndex�֑J�ڒ��A_nextPlayingIndex�Ŋ��荞�݂��Ȃ�
                if (_nextPlayingIndex >= 0 && _playables[_nextPlayingIndex].GetAnimationClip().name == nextClip) return;

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
