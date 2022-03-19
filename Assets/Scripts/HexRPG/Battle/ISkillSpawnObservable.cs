
namespace HexRPG.Battle
{
    using Skill;
    public interface ISkillSpawnObservable
    {
        ISkillComponentCollection[] SkillList { get; }
        bool IsAllSkillSpawned { get; }
    }
}
