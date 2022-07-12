using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Stage.Tower
{
    using Enemy;

    public class TowerInstaller : MonoInstaller, IEnemySpawnSettings
    {
        IReadOnlyList<DynamicSpawnSetting> IEnemySpawnSettings.DynamicEnemySpawnSettings => _dynamicEnemySpawnSettings;
        IReadOnlyList<StaticSpawnSetting> IEnemySpawnSettings.StaticEnemySpawnSettings => _staticEnemySpawnSettings;

        [Header("ìÆìIEnemy Spawn ê›íË")]
        [SerializeField] DynamicSpawnSetting[] _dynamicEnemySpawnSettings;

        [Header("ê√ìIEnemy Spawn ê›íË")]
        [SerializeField] StaticSpawnSetting[] _staticEnemySpawnSettings;

        [Header("Enemy Spawn Root")]
        [SerializeField] Transform _enemySpawnRoot;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<Health>().AsSingle();
            Container.BindInterfacesTo<TowerDamagedApplicable>().AsSingle();

            foreach (var setting in _dynamicEnemySpawnSettings)
            {
                var maxSize = setting.MaxCount;
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromPoolableMemoryPool<Transform, Vector3, EnemyOwner, EnemyOwner.Pool>(pool => pool
                        .WithInitialSize(maxSize)
                        .FromSubContainerResolve()
                        .ByNewContextPrefab(setting.Prefab)
                        .UnderTransform(_enemySpawnRoot));
            }

            foreach(var setting in _staticEnemySpawnSettings)
            {
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<EnemyInstaller>(setting.Prefab)
                    .UnderTransform(_enemySpawnRoot);
            }
        }
    }
}
