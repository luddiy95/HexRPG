
namespace HexRPG.Battle
{
    public interface IAttackComponentCollection : ICharacterComponentCollection
    {
        IAttackApplicator AttackApplicator { get; }
        IAttackController AttackController { get; }
        IAttackObservable AttackObservable { get; }
        IDamageApplicable DamageApplicable { get; }
        ILiberateObservable LiberateObservable { get; }
    }
}
