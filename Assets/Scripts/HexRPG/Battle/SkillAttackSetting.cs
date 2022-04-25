using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillAttackSetting : IAttackSetting
    {
        Hex[] AttackRange { get; }
    }

    public class SkillAttackSetting : ISkillAttackSetting
    {
        int IAttackSetting.Power => power;
        public int power;

        Hex[] ISkillAttackSetting.AttackRange => attackRange;
        public Hex[] attackRange;
    }
}
