using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cinemachine;

namespace HexRPG.Battle
{
    using Player;
    using Enemy;
    using Stage;

    public class BattleManager : MonoBehaviour, IBattleObservable
    {
        IUpdater _updater;
        IUpdateObservable _updateObservable;
        PlayerOwner.Factory _playerFactory;
        List<EnemyOwner.Factory> _enemyFactories;
        ISpawnSettings _spawnSettings;

        IReadOnlyReactiveProperty<IPlayerComponentCollection> IBattleObservable.OnPlayerSpawn => _onPlayerSpawn;
        readonly IReactiveProperty<IPlayerComponentCollection> _onPlayerSpawn = new ReactiveProperty<IPlayerComponentCollection>();

        IObservable<IEnemyComponentCollection> IBattleObservable.OnEnemySpawn => _onEnemySpawn;
        readonly ISubject<IEnemyComponentCollection> _onEnemySpawn = new Subject<IEnemyComponentCollection>();

        IObservable<Unit> IBattleObservable.OnBattleStart => _onBattleStart;
        ISubject<Unit> _onBattleStart = new Subject<Unit>();

        IPlayerComponentCollection _playerOwner;
        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        List<IEnemyComponentCollection> IBattleObservable.EnemyList => _enemyList;
        List<IEnemyComponentCollection> _enemyList;

        CinemachineBrain IBattleObservable.CinemachineBrain => _cinemachineBrain;
        [SerializeField] CinemachineBrain _cinemachineBrain;

        CinemachineVirtualCamera IBattleObservable.MainVirtualCamera => _mainVirtualCamera;
        [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;

        [SerializeField] Transform _enemyRoot;

        [Inject]
        public void Construct(
            IUpdater updater,
            IUpdateObservable updateObservable,
            PlayerOwner.Factory playerFactory,
            List<EnemyOwner.Factory> enemyFactories,
            ISpawnSettings spawnSettings)
        {
            _updater = updater;
            _updateObservable = updateObservable;
            _playerFactory = playerFactory;
            _enemyFactories = enemyFactories;
            _spawnSettings = spawnSettings;
        }

        void Start()
        {
            PlayStartSequence(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid PlayStartSequence(CancellationToken token)
        {
            await UniTask.Yield(token); // HUD, UIの初期化処理が終わってから(OnPlayerSpawnは良いがEnemyは複数いるためOnEnemySpawnがHUD, UIの初期化前に発行されたら意味がない)

            await SpawnPlayer();

            await SpawnEnemies();

            _onBattleStart.OnNext(Unit.Default);
            
            this.UpdateAsObservable() // 更新処理開始
                .Subscribe(_ => _updater.FireUpdateStreams())
                .AddTo(this);
        }

        async UniTask SpawnPlayer()
        {
            var playerSpawnSetting = _spawnSettings.PlayerSpawnSetting;
            _playerOwner = _playerFactory.Create(null, playerSpawnSetting.SpawnHex.transform.position);

            var memberController = _playerOwner.MemberController;
            await memberController.SpawnAllMember();
            memberController.ChangeMember(0);

            // Playerの位置を監視
            _playerLandedHex = _playerOwner.TransformController.GetLandedHex();
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _playerLandedHex = _playerOwner.TransformController.GetLandedHex();
                })
                .AddTo(this);

            _onPlayerSpawn.Value = _playerOwner;
        }

        async UniTask SpawnEnemies()
        {
            _enemyList = _spawnSettings.EnemySpawnSettings
                .Select((setting, index) => _enemyFactories[index].Create(_enemyRoot, setting.SpawnHex.transform.position) as IEnemyComponentCollection).ToList();

            await UniTask.WaitUntil(() => _enemyList.All(enemy => enemy.SkillSpawnObservable.IsAllSkillSpawned));

            _enemyList.ForEach(enemy => _onEnemySpawn.OnNext(enemy));
        }
    }
}
