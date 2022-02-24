using System.Collections.Generic;

namespace HexRPG.Battle
{
    using Stage;
    using Skill;

    public interface ISkillController
    {
        ISkillComponentCollection[] SkillList { get; }

        bool TryStartSkill(int index);
        void StartSkill(List<Hex> attackRange);

        ISkillComponentCollection RunningSkill { get; }
    }
}
