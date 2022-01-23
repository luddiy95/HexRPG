
namespace HexRPG.Battle
{
    using Skill;

    public interface ISkillController : IFeature
    {
        bool TryStartSkill(int index);

        BaseSkill RunningSkill { get; }
        void FinishSkill();
    }
}
