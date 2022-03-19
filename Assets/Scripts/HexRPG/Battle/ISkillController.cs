using System.Collections.Generic;

namespace HexRPG.Battle
{
    using Stage;
    using Skill;

    public interface ISkillController
    {
        ISkillComponentCollection StartSkill(int index, List<Hex> skillRange);
    }
}
