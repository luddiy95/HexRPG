using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillSetting
    {
        Sprite Icon { get; }
        int Cost { get; }
    }

    public class SkillSetting : MonoBehaviour, ISkillSetting
    {
        Sprite ISkillSetting.Icon => _icon;
        [SerializeField] Sprite _icon;
        int ISkillSetting.Cost => _cost;
        [SerializeField] int _cost;
    }
}
