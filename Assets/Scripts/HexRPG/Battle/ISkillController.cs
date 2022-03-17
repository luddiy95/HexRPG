using System.Collections.Generic;

namespace HexRPG.Battle
{
    using Stage;
    using Skill;

    public interface ISkillController
    {
        ISkillComponentCollection[] SkillList { get; }

        ISkillComponentCollection StartSkill(int index, List<Hex> skillRange);
    }
}
