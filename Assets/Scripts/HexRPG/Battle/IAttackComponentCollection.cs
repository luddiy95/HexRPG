
namespace HexRPG.Battle
{
    public interface IAttackComponentCollection : ICharacterComponentCollection
    {
        IAttackController AttackController { get; }
        IAttackObservable AttackObservable { get; }
        IDamageApplicable DamageApplicable { get; }
    }
}
