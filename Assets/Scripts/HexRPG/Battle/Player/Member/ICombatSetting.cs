using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle
{
    public enum CombatType
    {
        PROXIMITY, // ãﬂê⁄
        PROJECTILE // îÚÇ—ìπãÔ
    }

    public interface ICombatEquipment
    {
        GameObject EquipmentPrefab { get; }

        CombatType CombatType { get; }
        GameObject CombatPrefab { get; }
        
        Transform SpawnRoot { get; }
        PlayableAsset Timeline { get; }
    }
}
