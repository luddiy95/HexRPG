using UnityEngine;


namespace HexRPG.Battle.Combat
{
    public interface ICombatSetting
    {
        Sprite Icon { get; }
        string HitAudioName { get; }
    }

    public class CombatSetting : MonoBehaviour, ICombatSetting
    {
        Sprite ICombatSetting.Icon => _icon;
        [SerializeField] Sprite _icon;

        string ICombatSetting.HitAudioName => _hitAudioName;
        [SerializeField] string _hitAudioName;
    }
}
