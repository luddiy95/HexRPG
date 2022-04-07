using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace AnimationTest
{
    public class AnimationController : MonoBehaviour
    {
        IAnimationPlayer _animationPlayer;

        int _locomotionIndex = -1;

        private void Start()
        {
            TryGetComponent(out _animationPlayer);
        }

        private void Update()
        {
            var direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            var prevLocomotionIndex = _locomotionIndex;

            if(direction.magnitude > 0.1)
            {
                var euler = Quaternion.LookRotation(direction).eulerAngles.y;
                _locomotionIndex = ((int)((euler + 22.5) / 45)) % 8 + 1;
            }
            else
            {
                _locomotionIndex = 0;
            }

            //if(prevLocomotionIndex != _locomotionIndex) _animationPlayer.Play(Enum.GetNames(typeof(LocomotionType))[_locomotionIndex].ToString());
        }
    }
}
