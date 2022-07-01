using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Combat
{
    public interface ICombat
    {
        void Init(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline);
        Transform AttackColliderRoot { set; }
        void Execute();

        PlayableAsset PlayableAsset { get; }
        Vector3 Velocity { get; }
    }
}
