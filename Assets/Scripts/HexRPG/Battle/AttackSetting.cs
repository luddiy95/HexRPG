
namespace HexRPG.Battle
{
    public interface IAttackSetting
    {
        int Power { get; }
        //TODO: ‘®«‚È‚Ç
    }

    public class AttackSetting : IAttackSetting
    {
        int IAttackSetting.Power => _power;
        public int _power;
    }
}