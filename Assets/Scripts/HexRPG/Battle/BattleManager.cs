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

        IObservable<IPlayerComponentCollection> IBattleObservable.OnPlayerSpawn => _onPlayerSpawn;
        readonly ISubject<IPlayerComponentCollection> _onPlayerSpawn = new Subject<IPlayerComponentCollection>();

        IObservable<IEnemyComponentCollection> IBattleObservable.OnEnemySpawn => _onEnemySpawn;
        readonly ISubject<IEnemyComponentCollection> _onEnemySpawn = new Subject<IEnemyComponentCollection>();

        IObservable<Unit> IBattleObservable.OnBattleStart => _onBattleStart;
        ISubject<Unit> _onBattleStart = new Subject<Unit>();

        IPlayerComponentCollection _playerOwner;
        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        IEnemyComponentCollection[] _enemyOwnerList;
        List<Hex> IBattleObservable.EnemyLandedHexList => _enemyLandedHexList;
        List<Hex> _enemyLandedHexList = new List<Hex>();

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
            await UniTask.Yield(token); // HUD, UIの初期化処理が終わってから

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
            IPlayerComponentCollection _playerOwner = _playerFactory.Create(null, playerSpawnSetting.SpawnHex.transform.position);

            var memberController = _playerOwner.MemberController;
            await memberController.SpawnAllMember();
            memberController.ChangeMember(0);

            // Playerの位置を監視
            var transformController = _playerOwner.TransformController;
            _playerLandedHex = transformController.GetLandedHex();
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _playerLandedHex = transformController.GetLandedHex();
                })
                .AddTo(this);

            _onPlayerSpawn.OnNext(_playerOwner);
        }

        async UniTask SpawnEnemies()
        {
            _enemyOwnerList = _spawnSettings.EnemySpawnSettings
                .Select((setting, index) => _enemyFactories[index].Create(_enemyRoot, setting.SpawnHex.transform.position) as IEnemyComponentCollection).ToArray();

            await UniTask.WaitUntil(() => _enemyOwnerList.All(enemy => enemy.SkillSpawnObservable.IsAllSkillSpawned));

            // 各Enemyの位置を監視
            Array.ForEach(_enemyOwnerList, enemy => _enemyLandedHexList.Add(enemy.TransformController.GetLandedHex()));
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    for(int i = 0; i < _enemyOwnerList.Length; i++)
                    {
                        _enemyLandedHexList[i] = _enemyOwnerList[i].TransformController.GetLandedHex();
                    }
                })
                .AddTo(this);

            Array.ForEach(_enemyOwnerList, enemy => _onEnemySpawn.OnNext(enemy));
        }
    }
}
