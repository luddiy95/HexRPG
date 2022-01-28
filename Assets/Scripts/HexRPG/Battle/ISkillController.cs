using System.Collections.Generic;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillController : IFeature
    {
        ICustomComponentCollection[] SkillList { get; }

        bool TryStartSkill(int index, List<Hex> attackRange);

        ICustomComponentCollection RunningSkill { get; }

        void StartSkillEffect();

        void FinishSkill();
    }
}
