using UnityEngine;

namespace HexRPG.Battle
{
    public interface IAnimatorController
    {
        Animator Animator { get; }

        void Pause();
        void Restart();
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

        public static void SetTrigger(this IAnimatorController animatorController, string trigger)
        {
            animatorController.SetTrigger(trigger, trigger);
        }

        public static void SetTrigger(this IAnimatorController animatorController, string trigger, string destinationState, int layerIndex = 0)
        {
            //TODO: StateëJà⁄íÜÇÕSetTriggerÇµÇ»Ç¢ó}êßÇ‡ïKóvÅH
            var animator = animatorController.Animator;
            if (animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(destinationState)) return;
            animator.SetTrigger(trigger);
        }
    }
}
