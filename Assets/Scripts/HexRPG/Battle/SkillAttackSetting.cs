using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillAttackSetting : IAttackSetting
    {
        List<Hex> AttackRange { get; }
    }

    public class SkillAttackSetting : ISkillAttackSetting
    {
        int IAttackSetting.Power => _power;
        public int _power;

        List<Hex> ISkillAttackSetting.AttackRange => _attackRange;
        public List<Hex> _attackRange;
    }
}
