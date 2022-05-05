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
        readonly ISubject<Unit> _onBattleStart = new Subject<Unit>();

        IPlayerComponentCollection _playerOwner;
        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        IReadOnlyReactiveCollection<IEnemyComponentCollection> IBattleObservable.EnemyList => _enemyList;
        IReactiveCollection<IEnemyComponentCollection> _enemyList = new ReactiveCollection<IEnemyComponentCollection>();

        CinemachineBrain IBattleObservable.CinemachineBrain => _cinemachineBrain;
        [SerializeField] CinemachineBrain _cinemachineBrain;

        CinemachineVirtualCamera IBattleObservable.MainVirtualCamera => _mainVirtualCamera;
        [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;

        [SerializeField] CinemachineTargetGroup _targetGroup;

        [SerializeField] Transform _playerRoot;
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

            await SpawnPlayer(token);

            await SpawnEnemies(token);

            _onBattleStart.OnNext(Unit.Default);
            
            this.UpdateAsObservable() // 更新処理開始
                .Subscribe(_ => _updater.FireUpdateStreams())
                .AddTo(this);
        }

        async UniTask SpawnPlayer(CancellationToken token)
        {
            var playerSpawnSetting = _spawnSettings.PlayerSpawnSetting;
            _playerOwner = _playerFactory.Create(_playerRoot, playerSpawnSetting.SpawnHex.transform.position);

            var memberController = _playerOwner.MemberController;
            await memberController.SpawnAllMember(token);
            memberController.ChangeMember(0);

            _playerOwner.CharacterActionStateController.Init(); // 諸々の初期化が終わってからActionStateControllerを初期化

            _targetGroup.m_Targets[0].target = _playerOwner.TransformController.MoveTransform;
            // Playerの位置を監視
            _playerLandedHex = _playerOwner.TransformController.GetLandedHex();
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _playerLandedHex = _playerOwner.TransformController.GetLandedHex();
                })
                .AddTo(this);

            _onPlayerSpawn.OnNext(_playerOwner);
        }

        async UniTask SpawnEnemies(CancellationToken token)
        {
            var enemySpawnSettings = _spawnSettings.EnemySpawnSettings;
            for(int i = 0; i < enemySpawnSettings.Length; i++)
            {
                _enemyList.Add(_enemyFactories[i].Create(_enemyRoot, enemySpawnSettings[i].SpawnHex.transform.position));
            }

            await UniTask.WaitUntil(() => _enemyList.All(enemy => enemy.SkillSpawnObservable.IsAllSkillSpawned), cancellationToken: token);

            foreach (var enemy in _enemyList) enemy.AnimationController.Init();
            foreach (var enemy in _enemyList) enemy.CharacterActionStateController.Init(); // 諸々の初期化が終わってからActionStateControllerを初期化した方が良い

            // enemyが死んだらListからRemove
            foreach(var enemy in _enemyList)
            {
                enemy.DieObservable.OnFinishDie
                    .Subscribe(_ => _enemyList.Remove(enemy))
                    .AddTo(this);
            }

            foreach (var enemy in _enemyList) _onEnemySpawn.OnNext(enemy);
        }
    }
}
