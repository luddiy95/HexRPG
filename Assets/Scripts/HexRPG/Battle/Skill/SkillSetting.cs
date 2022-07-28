using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillSetting
    {
        Attribute Attribute { get; }
        Sprite Icon { get; }
        string HitAudioName { get; }

        int Cost { get; }
        void SetCost(int cost);
    }

    public class SkillSetting : MonoBehaviour, ISkillSetting
    {
        Attribute ISkillSetting.Attribute => _attribute;
        [SerializeField] Attribute _attribute;
        Sprite ISkillSetting.Icon => _icon;
        [SerializeField] Sprite _icon;
        string ISkillSetting.HitAudioName => _hitAudioName;
        [SerializeField] string _hitAudioName;

        int _cost;
        int ISkillSetting.Cost => _cost;
        void ISkillSetting.SetCost(int cost)
        {
            _cost = cost;
        }
    }
}
