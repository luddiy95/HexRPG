using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
        enum GameResultType
        {
            NONE,
            WIN,
            LOSE
        }

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
        readonly ISubject<Unit> _onBattleStart = new Subject<Unit>();

        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        IReadOnlyReactiveCollection<IEnemyComponentCollection> IBattleObservable.EnemyList => _enemyList;
        readonly IReactiveCollection<IEnemyComponentCollection> _enemyList = new ReactiveCollection<IEnemyComponentCollection>();

        CinemachineBrain IBattleObservable.CinemachineBrain => _cinemachineBrain;
        [SerializeField] CinemachineBrain _cinemachineBrain;

        CinemachineVirtualCamera IBattleObservable.MainVirtualCamera => _mainVirtualCamera;
        [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;

        [SerializeField] CinemachineTargetGroup _targetGroup;

        [SerializeField] Transform _playerRoot;
        [SerializeField] Transform _enemyRoot;

        [SerializeField] NavMeshSurface _enemySurface;

        GameResultType _resultType = GameResultType.NONE;

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
            PlayGameSequence(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid PlayGameSequence(CancellationToken token)
        {
            await PlayStartSequence(token);

            SetFinishGameRule();

            await UniTask.WaitWhile(() => _resultType == GameResultType.NONE);

            PlayEndSequence().Forget();
        }

        async UniTask PlayStartSequence(CancellationToken token)
        {
            await UniTask.Yield(token); // HUD, UIの初期化処理が終わってから(OnPlayerSpawnは良いがEnemyは複数いるためOnEnemySpawnがHUD, UIの初期化前に発行されたら意味がない)

            _enemySurface.BuildNavMesh();

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
            var playerOwner = _playerFactory.Create(_playerRoot, playerSpawnSetting.SpawnHex.transform.position) as IPlayerComponentCollection;

            var memberController = playerOwner.MemberController;
            await memberController.SpawnAllMember(token);
            memberController.ChangeMember(0); //! ここでようやくCurMemberが発行される

            playerOwner.CharacterActionStateController.Init(); // 諸々の初期化が終わってからActionStateControllerを初期化

            _targetGroup.m_Targets[0].target = playerOwner.TransformController.MoveTransform;
            // Playerの位置を監視
            _playerLandedHex = playerOwner.TransformController.GetLandedHex();
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _playerLandedHex = playerOwner.TransformController.GetLandedHex();
                })
                .AddTo(this);
            // PlayerがLiberateしたらNavMeshを再Bake
            playerOwner.LiberateObservable.SuccessLiberateHexList
                .Subscribe(_ => _enemySurface.BuildNavMesh())
                .AddTo(this);

            _onPlayerSpawn.Value = playerOwner;
        }

        async UniTask SpawnEnemies(CancellationToken token)
        {
            var enemySpawnSettings = _spawnSettings.EnemySpawnSettings;
            for(int i = 0; i < enemySpawnSettings.Length; i++)
            {
                var enemy = _enemyFactories[i].Create(_enemyRoot, enemySpawnSettings[i].SpawnHex.transform.position);
                _enemyList.Add(enemy);

                // enemyが死んだらListからRemove
                if (enemy is IEnemyComponentCollection enemyOwner)
                {
                    enemyOwner.DieObservable.OnFinishDie
                        .Subscribe(_ =>
                        {
                            _enemyList.Remove(enemyOwner);
                            Destroy(enemy.gameObject);
                        })
                        .AddTo(this);
                }
            }

            await UniTask.WaitUntil(() => _enemyList.All(enemy => enemy.CombatSpawnObservable.isCombatSpawned), cancellationToken: token);
            await UniTask.WaitUntil(() => _enemyList.All(enemy => enemy.SkillSpawnObservable.IsAllSkillSpawned), cancellationToken: token);

            foreach (var enemy in _enemyList) enemy.AnimationController.Init();
            foreach (var enemy in _enemyList) enemy.CharacterActionStateController.Init(); // 諸々の初期化が終わってからActionStateControllerを初期化した方が良い

            foreach (var enemy in _enemyList) _onEnemySpawn.OnNext(enemy);
        }

        async UniTask PlayEndSequence()
        {
            switch (_resultType)
            {
                case GameResultType.WIN: Debug.Log("WIN"); break;
                case GameResultType.LOSE: Debug.Log("LOSE"); break;
            }
        }

        void SetFinishGameRule()
        {
            _onPlayerSpawn.Value.DieObservable.OnFinishDie
                .Subscribe(_ => _resultType = GameResultType.LOSE)
                .AddTo(this);

            _enemyList.ObserveCountChanged()
                .Where(_ => _enemyList.Count == 0)
                .Subscribe(_ => _resultType = GameResultType.WIN)
                .AddTo(this);
        }
    }
}
