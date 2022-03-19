using UnityEngine;

namespace HexRPG.Battle
{
    public class AnimatorBehaviour : MonoBehaviour, IAnimatorController
    {
        protected IAnimatorController Self => this;

        Animator IAnimatorController.Animator => _animator != null ? _animator : GetComponent<Animator>();
        [Header("動かすAnimator。null ならこのオブジェクト。")]
        [SerializeField] protected Animator _animator;

        float _animatorSpeedCache = 0f;

        void IAnimatorController.Pause()
        {
            _animatorSpeedCache = Self.Animator.speed;
            Self.Animator.speed = 0;
        }

        void IAnimatorController.Restart()
        {
            Self.Animator.speed = _animatorSpeedCache;
        }
    }
}