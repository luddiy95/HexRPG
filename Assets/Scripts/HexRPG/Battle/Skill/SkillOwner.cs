using Zenject;

namespace HexRPG.Battle.Skill
{
    public interface ISkillComponentCollection : IBaseComponentCollection
    {
        ISkillSetting SkillSetting { get; }
        ISkill Skill { get; }
        ISkillObservable SkillObservable { get; }
    }

    public class SkillOwner : AbstractOwner<SkillOwner>, ISkillComponentCollection
    {
        [Inject] ISkillSetting ISkillComponentCollection.SkillSetting { get; }
        [Inject] ISkill ISkillComponentCollection.Skill { get; }
        [Inject] ISkillObservable ISkillComponentCollection.SkillObservable { get; }
    }
}
