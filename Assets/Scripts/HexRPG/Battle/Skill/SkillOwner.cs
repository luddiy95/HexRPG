using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Skill
{
    public interface ISkillComponentCollection
    {
        public ISkillSetting SkillSetting { get; }
        public ISkill Skill { get; }
        public IAttackSkill AttackSkill { get; }
    }

    public class SkillOwner : MonoBehaviour, ISkillComponentCollection
    {
        ISkillSetting ISkillComponentCollection.SkillSetting => _skillSetting;
        ISkillSetting _skillSetting;
        ISkill ISkillComponentCollection.Skill => _skill;
        ISkill _skill;
        IAttackSkill ISkillComponentCollection.AttackSkill => _attackSkill;
        IAttackSkill _attackSkill;

        [Inject]
#nullable enable
        public void Construct(
            ITransformController transformController, 
            ISkillSetting skillSetting, 
            ISkill skill,
            IAttackSkill? attackSkill = null
        )
        {
            _skillSetting = skillSetting;
            _skill = skill;
            _attackSkill = attackSkill;
        }
#nullable disable

        public class Factory : PlaceholderFactory<Transform, Vector3, SkillOwner>
        {

        }
    }
}
