
namespace HexRPG.Battle
{
    public interface ISkillController : IFeature
    {
        ICustomComponentCollection[] SkillList { get; }

        bool TryStartSkill(int index);

        ICustomComponentCollection RunningSkill { get; }
        void FinishSkill();
    }
}
