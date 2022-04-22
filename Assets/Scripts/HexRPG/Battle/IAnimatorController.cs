using UnityEngine;

namespace HexRPG.Battle
{
    public interface IAnimatorController
    {
        Animator Animator { get; }

        void Pause();
        void Restart();
    }
}
