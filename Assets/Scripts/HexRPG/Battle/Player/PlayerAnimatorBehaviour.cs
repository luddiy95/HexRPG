using UniRx;
using UnityEngine;
using Zenject;
using UniRx.Triggers;
using System;

namespace HexRPG.Battle.Player
{
    public class PlayerAnimatorBehaviour : MonoBehaviour, IAnimatorController
    {
        IMemberObservable _memberObservable;

        Animator IAnimatorController.Animator => _animator;
        [Header("動かすAnimator。null ならこのオブジェクト。")]
        [SerializeField] Animator _animator;

#nullable enable
        IDisposable? _moveAnimationEnterDisposable = null;
        IDisposable? _moveAnimationExitDisposable = null;
#nullable disable

        const string MOVE = "Move";

        [Inject]
        public void Construct(IMemberObservable memberObservable)
        {
            _memberObservable = memberObservable;
        }

        void Start()
        {
            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(memberOwner =>
                {
#nullable enable
                    _moveAnimationEnterDisposable?.Dispose();
                    _moveAnimationExitDisposable?.Dispose();
#nullable disable

                    _animator = memberOwner.AnimatorController.Animator;
                    var stateMachineTrigger = _animator.GetBehaviour<ObservableStateMachineTrigger>();
                    _moveAnimationEnterDisposable = stateMachineTrigger
                        .OnStateEnterAsObservable()
                        .Where(x => x.StateInfo.IsTag(MOVE))
                        .Subscribe(_ =>
                        {
                            _animator.SetSpeed(0, 0);
                        });

                    _moveAnimationExitDisposable = stateMachineTrigger
                        .OnStateExitAsObservable()
                        .Where(x => x.StateInfo.IsTag(MOVE))
                        .Subscribe(_ =>
                        {
                        });
                })
                .AddTo(this);
        }
    }
}
