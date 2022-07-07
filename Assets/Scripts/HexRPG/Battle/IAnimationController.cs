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
        None,

        Idle,
        Move,
        Rotate,

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

        public static string[] RotateClips => new string[]
        {
            "RotateRight", "RotateLeft"
        };
    }
}
