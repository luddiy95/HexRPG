
namespace HexRPG.Battle
{
    public interface IHostileComponentCollection
    {
        IAttackController AttackController { get; }
        IAttackObservable AttackObservable { get; }
        IDamageApplicable DamageApplicable { get; }
    }
}
