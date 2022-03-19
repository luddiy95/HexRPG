using UniRx;
using Zenject;
using UniRx.Triggers;
using System;

namespace HexRPG.Battle.Player
{
    public class PlayerAnimatorBehaviour : AnimatorBehaviour
    {
        IMemberObservable _memberObservable;

#nullable enable
        IDisposable? _moveAnimationEnterDisposable = null;
        IDisposable? _moveAnimationExitDisposable = null;
#nullable disable

        const string MOVE = "Move";

        [Inject]
        public void Construct(
            IMemberObservable memberObservable
        )
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
                    var stateMachineTrigger = Self.Animator.GetBehaviour<ObservableStateMachineTrigger>();
                    _moveAnimationEnterDisposable = stateMachineTrigger
                        .OnStateEnterAsObservable()
                        .Where(x => x.StateInfo.IsTag(MOVE))
                        .Subscribe(_ =>
                        {
                            Self.Animator.SetSpeed(0, 0);
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
