using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillSetting
    {
        Sprite Icon { get; }

        int Cost { get; }
        void SetCost(int cost);
    }

    public class SkillSetting : MonoBehaviour, ISkillSetting
    {
        Sprite ISkillSetting.Icon => _icon;
        [SerializeField] Sprite _icon;

        int _cost;
        int ISkillSetting.Cost => _cost;
        void ISkillSetting.SetCost(int cost)
        {
            _cost = cost;
        }
    }
}
