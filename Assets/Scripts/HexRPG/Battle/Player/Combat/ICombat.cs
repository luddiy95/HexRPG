using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Combat
{
    public interface ICombat
    {
        void Init(PlayableAsset timeline, ICharacterComponentCollection combatOrigin, IAnimationController memberAnimationController);
        void Execute();

        PlayableAsset PlayableAsset { get; }
        Vector3 Velocity { get; }
    }
}
