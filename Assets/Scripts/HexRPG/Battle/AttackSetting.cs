
namespace HexRPG.Battle
{
    public interface IAttackSetting
    {
        int Power { get; }
        //TODO: �����Ȃ�
    }

    public class AttackSetting : IAttackSetting
    {
        int IAttackSetting.Power => power;
        public int power;
    }
}