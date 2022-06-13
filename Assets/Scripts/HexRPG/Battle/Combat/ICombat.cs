using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Combat
{
    public interface ICombat
    {
        void Init(PlayableAsset timeline, IAttackApplicator attackApplicator, IAnimationController memberAnimationController);
        void Execute();

        PlayableAsset PlayableAsset { get; }
        Vector3 Velocity { get; }
    }
}
