using System;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle
{
    using Enemy;

    public class TowerInstaller : MonoInstaller, IEnemySpawnSettings
    {
        DynamicSpawnSetting[] IEnemySpawnSettings.DynamicEnemySpawnSettings => _dynamicEnemySpawnSettings;
        StaticSpawnSetting[] IEnemySpawnSettings.StaticEnemySpawnSettings => _staticEnemySpawnSettings;

        [Header("動的Enemy Spawn 設定")]
        [SerializeField] DynamicSpawnSetting[] _dynamicEnemySpawnSettings;

        [Header("静的Enemy Spawn 設定")]
        [SerializeField] StaticSpawnSetting[] _staticEnemySpawnSettings;

        public override void InstallBindings()
        {
            Array.ForEach(_dynamicEnemySpawnSettings, setting =>
            {
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<EnemyInstaller>(setting.Prefab);
            });

            Array.ForEach(_staticEnemySpawnSettings, setting =>
            {
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<EnemyInstaller>(setting.Prefab);
            });
        }
    }
}
