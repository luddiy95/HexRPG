using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Skill
{
    public interface ISkillComponentCollection
    {
        ISkillSetting SkillSetting { get; }
        ISkill Skill { get; }
        ISkillObservable SkillObservable { get; }
    }

    public class SkillOwner : MonoBehaviour, ISkillComponentCollection
    {
        [Inject] ISkillSetting ISkillComponentCollection.SkillSetting { get; }
        [Inject] ISkill ISkillComponentCollection.Skill { get; }
        [Inject] ISkillObservable ISkillComponentCollection.SkillObservable { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, SkillOwner>
        {

        }
    }
}
