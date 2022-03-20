
namespace HexRPG.Battle
{
    public interface ICombatAttackSetting : IAttackSetting
    {
    }

    public class CombatAttackSetting : ICombatAttackSetting
    {
        int IAttackSetting.Power => _power;
        public int _power;
    }
}
