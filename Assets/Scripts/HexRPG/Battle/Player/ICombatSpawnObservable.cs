
namespace HexRPG.Battle.Player
{
    using Combat;

    public interface ICombatSpawnObservable
    {
        ICombatComponentCollection Combat { get; }
        bool isCombatSpawned { get; }
    }
}
