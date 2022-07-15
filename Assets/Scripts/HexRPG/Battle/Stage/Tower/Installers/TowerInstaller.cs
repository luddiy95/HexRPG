using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Stage.Tower
{
    using Enemy;

    public class TowerInstaller : MonoInstaller, IEnemySpawnSettings
    {
        IReadOnlyList<StaticSpawnSetting> IEnemySpawnSettings.StaticEnemySpawnSettings => _staticEnemySpawnSettings;
        IReadOnlyList<DynamicSpawnSetting> IEnemySpawnSettings.DynamicEnemySpawnSettings => _dynamicEnemySpawnSettings;

        [Header("ê√ìIEnemy Spawn ê›íË")]
        [SerializeField] StaticSpawnSetting[] _staticEnemySpawnSettings;

        [Header("ìÆìIEnemy Spawn ê›íË")]
        [SerializeField] DynamicSpawnSetting[] _dynamicEnemySpawnSettings;

        [Header("Enemy Spawn Root")]
        [SerializeField] Transform _enemySpawnRoot;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<Health>().AsSingle();
            Container.BindInterfacesTo<TowerDamagedApplicable>().AsSingle();

            foreach (var setting in _staticEnemySpawnSettings) BindFactory(1, setting.Prefab);
            foreach (var setting in _dynamicEnemySpawnSettings) BindFactory(setting.MaxCount, setting.Prefab);

            void BindFactory(int maxSize, GameObject prefab)
            {
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromPoolableMemoryPool<Transform, Vector3, EnemyOwner, EnemyOwner.Pool>(pool => pool
                        .WithInitialSize(maxSize)
                        .FromSubContainerResolve()
                        .ByNewContextPrefab(prefab)
                        .UnderTransform(_enemySpawnRoot));
            }
        }
    }
}
