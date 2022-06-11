using UnityEngine;
using System;
using Zenject;

namespace HexRPG.Battle
{
    using Player;
    using Player.HUD;
    using Enemy;
    using Enemy.HUD;
    using HUD;

    public class BattleInstaller : MonoInstaller, ISpawnSettings
    {
        SpawnSetting ISpawnSettings.PlayerSpawnSetting => _playerSpawnSetting;
        [Header("Playerê∂ê¨ê›íË")]
        [SerializeField] SpawnSetting _playerSpawnSetting;

        SpawnSetting[] ISpawnSettings.EnemySpawnSettings => _enemySpawnSettings;
        [Header("Enemyê∂ê¨ê›íË")]
        [SerializeField] SpawnSetting[] _enemySpawnSettings;

        [SerializeField] BattleData _battleData;

        [SerializeField] DisplayDataContainer _displayDataContainer;

        [SerializeField] GameObject _memberStatusPrefab;
        [SerializeField] GameObject _enemyStatusPrefab;
        [SerializeField] GameObject _damagedPanelPrefab;

        public override void InstallBindings()
        {
            Container.BindInterfacesTo<UpdateFeature>().AsSingle();
            Container.BindInterfacesTo<DeltaTime>().AsSingle();

            Container.Bind<BattleData>().FromInstance(_battleData);
            Container.Bind<DisplayDataContainer>().FromInstance(_displayDataContainer);

            Container.BindInterfacesTo<ScoreController>().AsSingle();

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
            Container.BindFactory<MemberStatusHUD, MemberStatusHUD.Factory>().FromComponentInNewPrefab(_memberStatusPrefab);
            Container.BindFactory<EnemyStatusHUD, EnemyStatusHUD.Factory>().FromComponentInNewPrefab(_enemyStatusPrefab);
            Container.BindFactory<DamagedPanelParentHUD, DamagedPanelParentHUD.Factory>().FromComponentInNewPrefab(_damagedPanelPrefab);
        }
    }
}
