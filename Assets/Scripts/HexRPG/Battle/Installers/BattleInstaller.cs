using UnityEngine;
using Zenject;

namespace HexRPG.Battle
{
    using Player;
    using Player.HUD;
    using Enemy.HUD;
    using HUD;

    public interface IPlayerSpawnSetting
    {
        StaticSpawnSetting SpawnSetting { get; }
    }

    public class BattleInstaller : MonoInstaller, IPlayerSpawnSetting
    {
        StaticSpawnSetting IPlayerSpawnSetting.SpawnSetting => _playerSpawnSetting;

        [Header("Player Spawn ê›íË")]
        [SerializeField] StaticSpawnSetting _playerSpawnSetting;

        [SerializeField] BattleData _battleData;

        [SerializeField] DisplayDataContainer _displayDataContainer;

        [SerializeField] GameObject _memberStatusPrefab;
        [SerializeField] GameObject _enemyStatusPrefab;
        [SerializeField] GameObject _damagedPanelPrefab;

        [SerializeField] Transform _enemyStatusHUDRoot;
        [SerializeField] Transform _damagedPanelParentRoot;

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

            Container.BindFactory<MemberStatusHUD, MemberStatusHUD.Factory>().FromComponentInNewPrefab(_memberStatusPrefab);
            Container.BindFactory<EnemyStatusHUD, EnemyStatusHUD.Factory>()
                .FromPoolableMemoryPool<EnemyStatusHUD, EnemyStatusHUD.Pool>(pool => pool
                    .FromComponentInNewPrefab(_enemyStatusPrefab).UnderTransform(_enemyStatusHUDRoot));
            Container.BindFactory<DamagedPanelParentHUD, DamagedPanelParentHUD.Factory>()
                .FromPoolableMemoryPool<DamagedPanelParentHUD, DamagedPanelParentHUD.Pool>(pool => pool
                    .FromComponentInNewPrefab(_damagedPanelPrefab).UnderTransform(_damagedPanelParentRoot));
        }
    }
}
