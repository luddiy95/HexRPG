
namespace HexRPG.Battle
{
    using Stage;
    using Skill;

    public interface ISkillController
    {
        ISkillComponentCollection StartSkill(int index, Hex landedHex = null, int skillRotation = 0);
    }
}
