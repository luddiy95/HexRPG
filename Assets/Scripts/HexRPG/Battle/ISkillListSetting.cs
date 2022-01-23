
namespace HexRPG.Battle
{
    using Skill;

    public interface ISkillListSetting : IFeature
    {
        BaseSkill[] SkillList { get; }
    }
}
