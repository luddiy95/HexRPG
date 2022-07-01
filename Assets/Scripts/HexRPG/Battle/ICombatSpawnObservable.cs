
namespace HexRPG.Battle
{
    using Combat;

    public interface ICombatSpawnObservable
    {
        ICombatComponentCollection Combat { get; }
        bool IsCombatSpawned { get; }
    }
}
