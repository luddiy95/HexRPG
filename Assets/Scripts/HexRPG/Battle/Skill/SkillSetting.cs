using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillSetting
    {
        Sprite Icon { get; }
        int MPcost { get; }
        int Damage { get; }
        public List<Vector2> Range { get; }
    }

    public class SkillSetting : MonoBehaviour, ISkillSetting
    {
        Sprite ISkillSetting.Icon => _icon;
        [SerializeField] Sprite _icon;
        int ISkillSetting.MPcost => _MPcost;
        [SerializeField] int _MPcost;
        int ISkillSetting.Damage => _damage;
        [SerializeField] int _damage;
        List<Vector2> ISkillSetting.Range => _range;
        [SerializeField] List<Vector2> _range;
    }
}
