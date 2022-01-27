using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillSetting : IFeature
    {
        Sprite Icon { get; }
        int MPcost { get; }
        int Damage { get; }
        public List<Vector2> Range { get; }
        public string SkillAnimationParam { get; }
    }

    public class SkillSetting : AbstractCustomComponentBehaviour, ISkillSetting
    {
        Sprite ISkillSetting.Icon => _icon;
        [SerializeField] Sprite _icon;
        int ISkillSetting.MPcost => _MPcost;
        [SerializeField] int _MPcost;
        int ISkillSetting.Damage => _damage;
        [SerializeField] int _damage;
        List<Vector2> ISkillSetting.Range => _range;
        [SerializeField] List<Vector2> _range;
        string ISkillSetting.SkillAnimationParam => _skillAnimationParam;
        [SerializeField] string _skillAnimationParam;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISkillSetting>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
