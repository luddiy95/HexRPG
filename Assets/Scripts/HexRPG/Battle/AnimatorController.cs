using UnityEngine;
using UniRx;

namespace HexRPG.Battle
{
    using Player;

    public interface IAnimatorController : IFeature
    {
        IReadOnlyReactiveProperty<Animator> CurAnimator { get; }
    }

    public class AnimatorController : AbstractCustomComponentBehaviour, IAnimatorController
    {
        IReadOnlyReactiveProperty<Animator> IAnimatorController.CurAnimator => Animator;

        IReactiveProperty<Animator> Animator = new ReactiveProperty<Animator>();
        [Header("動かすAnimator。null ならこのオブジェクト。")]
        [SerializeField] Animator _animator;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IAnimatorController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_animator == null) TryGetComponent(out _animator);
            Animator.Value = _animator;

            // Playerの場合
            if (Owner.QueryInterface(out IMemberObservable memberObservable))
            {
                memberObservable.CurMember
                    .Where(member => member != null)
                    .Subscribe(member =>
                    {
                        if (member.QueryInterface(out IAnimatorController animatorController))
                        {
                            Animator.Value = animatorController.CurAnimator.Value;
                        }
                    })
                    .AddTo(this);
            }
        }
    }

    public static class AnimatorExtensions
    {
        public static void SetTrigger(this IAnimatorController animatorController, string trigger)
        {
            animatorController.CurAnimator.Value.SetTrigger(trigger);
        }

        public static void ResetTrigger(this IAnimatorController animatorController, string trigger)
        {
            animatorController.CurAnimator.Value.ResetTrigger(trigger);
        }

        public static void SetSpeed(this IAnimatorController animatorController, float horizontal, float vertical)
        {
            animatorController.CurAnimator.Value.SetFloat("SpeedHorizontal", horizontal);
            animatorController.CurAnimator.Value.SetFloat("SpeedVertical", vertical);
        }
    }
}