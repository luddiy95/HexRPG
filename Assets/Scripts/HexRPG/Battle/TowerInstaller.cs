using System.Collections.Generic;
using System;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle
{
    using Enemy;

    public class TowerInstaller : MonoInstaller, IEnemySpawnSettings
    {
        IReadOnlyList<DynamicSpawnSetting> IEnemySpawnSettings.DynamicEnemySpawnSettings => _dynamicEnemySpawnSettings;
        IReadOnlyList<StaticSpawnSetting> IEnemySpawnSettings.StaticEnemySpawnSettings => _staticEnemySpawnSettings;

        [Header("動的Enemy Spawn 設定")]
        [SerializeField] DynamicSpawnSetting[] _dynamicEnemySpawnSettings;

        [Header("静的Enemy Spawn 設定")]
        [SerializeField] StaticSpawnSetting[] _staticEnemySpawnSettings;

        [Header("Enemy Spawn Root")]
        [SerializeField] Transform _enemySpawnRoot;

        public override void InstallBindings()
        {
            Array.ForEach(_dynamicEnemySpawnSettings, setting =>
            {
                var maxSize = setting.MaxCount;
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromPoolableMemoryPool<Transform, Vector3, EnemyOwner, EnemyOwner.Pool>(pool => pool
                        .WithInitialSize(maxSize)
                        .FromSubContainerResolve()
                        .ByNewContextPrefab(setting.Prefab)
                        .UnderTransform(_enemySpawnRoot));
            });

            Array.ForEach(_staticEnemySpawnSettings, setting =>
            {
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<EnemyInstaller>(setting.Prefab)
                    .UnderTransform(_enemySpawnRoot);
            });
        }
    }
}
