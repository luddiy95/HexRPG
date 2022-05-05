using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;
    using Skill;

    public interface ISkillController
    {
        ISkillComponentCollection StartSkill(int index, Hex skillCenter = null, int skillRotation = 0);
    }
}
