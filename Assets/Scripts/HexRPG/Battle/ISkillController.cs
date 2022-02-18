using System.Collections.Generic;

namespace HexRPG.Battle
{
    using Stage;
    using Skill;

    public interface ISkillController
    {
        ISkillComponentCollection[] SkillList { get; }

        bool TryStartSkill(int index, List<Hex> attackRange);

        ISkillComponentCollection RunningSkill { get; }

        void StartSkillEffect();

        void FinishSkill();
    }
}
