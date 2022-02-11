using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillController : IFeature
    {
        ICustomComponentCollection[] SkillList { get; }

        bool TryStartSkill(int index, List<Hex> attackRange, Animator animator);

        ICustomComponentCollection RunningSkill { get; }

        void StartSkillEffect();

        void FinishSkill();
    }
}
