using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;

namespace HexRPG.Battle
{
    public class AnimatorBehaviour : MonoBehaviour, IAnimatorController
    {
        Animator IAnimatorController.Animator { get { return _animator; } }
        [Header("動かすAnimator。null ならこのオブジェクト。")]
        [SerializeField] protected Animator _animator;

        float _animatorSpeedCache = 0f;

        void Start()
        {
            if (_animator == null) TryGetComponent(out _animator);
        }

        void IAnimatorController.Pause()
        {
            _animatorSpeedCache = _animator.speed;
            _animator.speed = 0;
        }

        void IAnimatorController.Restart()
        {
            _animator.speed = _animatorSpeedCache;
        }
    }
}