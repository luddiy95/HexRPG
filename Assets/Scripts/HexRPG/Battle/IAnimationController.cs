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
        Locomotion,
        Damaged,
        Combat,
        Skill
    }

    public static class AnimationExtensions
    {
        public static string[] LocomotionClips => new string[]
        {
            "Idle", "Movefwd", "Moverightfwd", "Moveright", "Moverightbwd", "Movebwd", "Moveleftbwd", "Moveleft", "Moveleftfwd"
        };
    }
}
