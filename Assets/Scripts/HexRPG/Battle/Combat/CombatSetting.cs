using UnityEngine;


namespace HexRPG.Battle.Combat
{
    public interface ICombatSetting
    {
        Sprite Icon { get; }
        int Damage { get; }
    }

    public class CombatSetting : MonoBehaviour, ICombatSetting
    {
        Sprite ICombatSetting.Icon => _icon;
        [SerializeField] Sprite _icon;

        int ICombatSetting.Damage => _damage;
        [SerializeField] int _damage;
    }
}
