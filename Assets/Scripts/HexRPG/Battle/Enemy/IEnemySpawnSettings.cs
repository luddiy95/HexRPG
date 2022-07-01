
namespace HexRPG.Battle.Enemy
{
    public interface IEnemySpawnSettings
    {
        DynamicSpawnSetting[] DynamicEnemySpawnSettings { get; }
        StaticSpawnSetting[] StaticEnemySpawnSettings { get; }
    }
}
