using UnityEngine;
using System;
using Zenject;

namespace HexRPG
{
    using Battle;
    using Battle.Player;

    public class HexRpgInstaller : MonoInstaller, ISpawnSettings
    {
        SpawnSetting ISpawnSettings.PlayerSpawnSetting => _playerSpawnSetting;
        [Header("Player生成設定")]
        [SerializeField] SpawnSetting _playerSpawnSetting;

        SpawnSetting[] ISpawnSettings.EnemySpawnSettings => _enemySpawnSettings;
        [Header("Enemy生成設定")]
        [SerializeField] SpawnSetting[] _enemySpawnSettings;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<UpdateFeature>().AsSingle();
            Container.BindInterfacesTo<DeltaTime>().AsSingle();

            Container.BindFactory<Transform, Vector3, PlayerOwner, PlayerOwner.Factory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<PlayerInstaller>(_playerSpawnSetting.Prefab);
            Array.ForEach(_enemySpawnSettings, setting =>
            {
                // TODO:
            });
        }
    }
}
