using UnityEngine;


namespace HexRPG.Battle.Player.Combat
{
    public interface ICombatSetting
    {
        int Damage { get; }
    }

    public class CombatSetting : MonoBehaviour, ICombatSetting
    {
        int ICombatSetting.Damage => _damage;
        [SerializeField] int _damage;
    }
}
