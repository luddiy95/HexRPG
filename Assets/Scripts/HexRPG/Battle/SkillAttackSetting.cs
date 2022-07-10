using System.Collections.Generic;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillAttackSetting : IAttackSetting
    {
        Attribute Attribute { get; }
        List<Hex> AttackRange { get; }
    }

    public class SkillAttackSetting : ISkillAttackSetting
    {
        int IAttackSetting.Power => power;
        public int power;

        Attribute ISkillAttackSetting.Attribute => attribute;
        public Attribute attribute;

        List<Hex> ISkillAttackSetting.AttackRange => attackRange;
        public List<Hex> attackRange;
    }
}
