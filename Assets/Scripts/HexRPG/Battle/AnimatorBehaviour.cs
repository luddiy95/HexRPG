using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;

namespace HexRPG.Battle
{
    public interface IAnimatorController
    {
        Animator Animator { get; }
    }

    public class AnimatorBehaviour : MonoBehaviour, IAnimatorController
    {
        Animator IAnimatorController.Animator { get { return _animator; } }
        [Header("動かすAnimator。null ならこのオブジェクト。")]
        [SerializeField] Animator _animator;

        void Start()
        {
            if (_animator == null) TryGetComponent(out _animator);
        }
    }

    public static class AnimatorExtensions
    {
        public static void SetSpeed(this Animator animator, float horizontal, float vertical)
        {
            animator.SetFloat("SpeedHorizontal", horizontal);
            animator.SetFloat("SpeedVertical", vertical);
        }

        public static void SetSpeed(this IAnimatorController animatorController, float horizontal, float vertical)
        {
            animatorController.Animator.SetFloat("SpeedHorizontal", horizontal);
            animatorController.Animator.SetFloat("SpeedVertical", vertical);
        }
    }
}