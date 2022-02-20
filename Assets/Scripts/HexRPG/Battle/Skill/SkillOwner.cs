using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Skill
{
    public interface ISkillComponentCollection
    {
        ISkillSetting SkillSetting { get; }
        ISkill Skill { get; }
        IAttackSkill AttackSkill { get; }
    }

    public class SkillOwner : MonoBehaviour, ISkillComponentCollection
    {
        [Inject] ISkillSetting ISkillComponentCollection.SkillSetting { get; }
        [Inject] ISkill ISkillComponentCollection.Skill { get; }
#nullable enable
        [Inject] IAttackSkill? ISkillComponentCollection.AttackSkill { get; }
#nullable disable

        public class Factory : PlaceholderFactory<Transform, Vector3, SkillOwner>
        {

        }
    }
}
