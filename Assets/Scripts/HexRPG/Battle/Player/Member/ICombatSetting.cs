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
        Transform EquipmentRoot { get; }

        CombatType CombatType { get; }
        GameObject CombatPrefab { get; }
        
        PlayableAsset Timeline { get; }
    }
}
