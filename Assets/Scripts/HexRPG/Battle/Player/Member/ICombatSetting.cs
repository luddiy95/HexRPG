using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle
{
    public enum CombatType
    {
        PROXIMITY, // �ߐ�
        PROJECTILE // ��ѓ���
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
