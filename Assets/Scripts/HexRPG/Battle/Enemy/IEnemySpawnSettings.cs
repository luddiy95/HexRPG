using System.Collections.Generic;

namespace HexRPG.Battle.Enemy
{
    public interface IEnemySpawnSettings
    {
        IReadOnlyList<DynamicSpawnSetting> DynamicEnemySpawnSettings { get; }
        IReadOnlyList<StaticSpawnSetting> StaticEnemySpawnSettings { get; }
    }
}
