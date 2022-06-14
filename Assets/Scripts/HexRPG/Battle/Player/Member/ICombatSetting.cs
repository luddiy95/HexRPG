using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle
{
    public interface ICombatEquipment
    {
        GameObject Prefab { get; }
        Transform SpawnRoot { get; }
        PlayableAsset Timeline { get; }
    }
}
