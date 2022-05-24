
namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillAttackSetting : IAttackSetting
    {
        Attribute Attribute { get; }
        Hex[] AttackRange { get; }
    }

    public class SkillAttackSetting : ISkillAttackSetting
    {
        int IAttackSetting.Power => power;
        public int power;

        Attribute ISkillAttackSetting.Attribute => attribute;
        public Attribute attribute;

        Hex[] ISkillAttackSetting.AttackRange => attackRange;
        public Hex[] attackRange;
    }
}
