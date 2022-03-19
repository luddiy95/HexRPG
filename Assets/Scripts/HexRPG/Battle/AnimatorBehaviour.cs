using UnityEngine;

namespace HexRPG.Battle
{
    public class AnimatorBehaviour : MonoBehaviour, IAnimatorController
    {
        protected IAnimatorController Self => this;

        Animator IAnimatorController.Animator => _animator != null ? _animator : GetComponent<Animator>();
        [Header("������Animator�Bnull �Ȃ炱�̃I�u�W�F�N�g�B")]
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