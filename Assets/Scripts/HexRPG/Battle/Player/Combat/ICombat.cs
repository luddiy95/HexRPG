using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Combat
{
    public interface ICombat
    {
        void Init(PlayableAsset timeline, ICharacterComponentCollection combatOrigin, Animator animator);
        void Execute();

        Vector3 Velocity { get; }
    }
}
