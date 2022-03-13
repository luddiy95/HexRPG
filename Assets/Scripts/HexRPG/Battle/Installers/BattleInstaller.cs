using UnityEngine;
using System;
using Zenject;

namespace HexRPG.Battle
{
    using Player;
    using Enemy;
    using HUD;

    public class BattleInstaller : MonoInstaller, ISpawnSettings
    {
        SpawnSetting ISpawnSettings.PlayerSpawnSetting => _playerSpawnSetting;
        [Header("Playerê∂ê¨ê›íË")]
        [SerializeField] SpawnSetting _playerSpawnSetting;

        SpawnSetting[] ISpawnSettings.EnemySpawnSettings => _enemySpawnSettings;
        [Header("Enemyê∂ê¨ê›íË")]
        [SerializeField] SpawnSetting[] _enemySpawnSettings;

        [SerializeField] BattleDataContainer _battleDataContainer;

        [SerializeField] GameObject _enemyHealthGaugePrefab;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<UpdateFeature>().AsSingle();
            Container.BindInterfacesTo<DeltaTime>().AsSingle();

            Container.Bind<BattleData>().FromInstance(_battleDataContainer.Data);

            Container.BindInterfacesTo<Pauser>().AsSingle();

            Container.BindFactory<Transform, Vector3, PlayerOwner, PlayerOwner.Factory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<PlayerInstaller>(_playerSpawnSetting.Prefab);
            Array.ForEach(_enemySpawnSettings, setting =>
            {
                Container.BindFactory<Transform, Vector3, EnemyOwner, EnemyOwner.Factory>()
                    .FromSubContainerResolve()
                    .ByNewContextPrefab<EnemyInstaller>(setting.Prefab);
            });
            Container.BindFactory<HealthGauge, HealthGauge.Factory>().FromComponentInNewPrefab(_enemyHealthGaugePrefab);
        }
    }
}
