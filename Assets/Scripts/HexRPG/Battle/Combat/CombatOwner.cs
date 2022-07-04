using Zenject;

namespace HexRPG.Battle.Combat
{
    public interface ICombatComponentCollection : IBaseComponentCollection
    {
        ICombat Combat { get; }
        ICombatSetting CombatSetting { get; }
        ICombatObservable CombatObservable { get; }
    }

    public class CombatOwner : AbstractOwner<CombatOwner>, ICombatComponentCollection
    {
        [Inject] ICombat ICombatComponentCollection.Combat { get; }
        [Inject] ICombatSetting ICombatComponentCollection.CombatSetting { get; }
        [Inject] ICombatObservable ICombatComponentCollection.CombatObservable { get; }
    }
}
