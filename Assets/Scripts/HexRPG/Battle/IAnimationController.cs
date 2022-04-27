using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface IAnimationController
    {
        void Init();
        void Play(string clip);

        IObservable<Unit> OnFinishDamaged { get; }

        IObservable<Unit> OnFinishCombat { get; }
        IObservable<Unit> OnFinishSkill { get; }
    }

    public enum AnimationType
    {
        Idle,
        Move,

        Damaged,

        Die,

        Combat,
        Skill
    }

    public static class AnimationExtensions
    {
        public static string IdleClip => "Idle";

        public static string[] MoveClips => new string[]
        {
            "Movefwd", "Moverightfwd", "Moveright", "Moverightbwd", "Movebwd", "Moveleftbwd", "Moveleft", "Moveleftfwd"
        };

        public static bool IsLocomotionType(this AnimationType type)
        {
            return type == AnimationType.Idle || type == AnimationType.Move;
        }
    }
}
