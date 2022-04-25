using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillSetting
    {
        Sprite Icon { get; }
        int MPcost { get; }
    }

    public class SkillSetting : MonoBehaviour, ISkillSetting
    {
        Sprite ISkillSetting.Icon => _icon;
        [SerializeField] Sprite _icon;
        int ISkillSetting.MPcost => _MPcost;
        [SerializeField] int _MPcost;
    }
}
