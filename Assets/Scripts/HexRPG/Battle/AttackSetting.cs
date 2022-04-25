
namespace HexRPG.Battle
{
    public interface IAttackSetting
    {
        int Power { get; }
        //TODO: ‘®«‚È‚Ç
    }

    public class AttackSetting : IAttackSetting
    {
        int IAttackSetting.Power => power;
        public int power;
    }
}