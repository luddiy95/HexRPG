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
    using Stage.Tower;

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
        IPlayerSpawnSetting _playerSpawnSetting;

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

        /// <summary>
        /// 各Enemyの「向かおうとしている目的地 or hexの真ん中で静止している状態のLandedHex」
        /// </summary>
        IEnumerable<Hex> IBattleObservable.EnemyDestinationHexList => _enemyDestinationHexList;
        readonly List<Hex> _enemyDestinationHexList = new List<Hex>(32);

        IObservable<ITowerComponentCollection> IBattleObservable.OnTowerInit => _onTowerInit;
        readonly ISubject<ITowerComponentCollection> _onTowerInit = new Subject<ITowerComponentCollection>();

        List<ITowerComponentCollection> IBattleObservable.TowerList => _towerList;
        List<ITowerComponentCollection> _towerList = new List<ITowerComponentCollection>(16);

        IObservable<Unit> IBattleObservable.OnUpdateNavMesh => _onUpdateNavMesh;
        readonly ISubject<Unit> _onUpdateNavMesh = new Subject<Unit>();

        CinemachineBrain IBattleObservable.CinemachineBrain => _cinemachineBrain;
        [SerializeField] CinemachineBrain _cinemachineBrain;

        CinemachineVirtualCamera IBattleObservable.MainVirtualCamera => _mainVirtualCamera;
        [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;

        CinemachineOrbitalTransposer IBattleObservable.CameraTransposer => _cameraTransposer;
        CinemachineOrbitalTransposer _cameraTransposer;

        [SerializeField] CinemachineTargetGroup _targetGroup;

        [SerializeField] Transform _playerRoot;

        [SerializeField] NavMeshSurface _enemySurface;

        GameResultType _resultType = GameResultType.NONE;

        [Inject]
        public void Construct(
            IUpdater updater,
            IUpdateObservable updateObservable,
            PlayerOwner.Factory playerFactory,
            IPlayerSpawnSetting playerSpawnSetting
        )
        {
            _updater = updater;
            _updateObservable = updateObservable;
            _playerFactory = playerFactory;
            _playerSpawnSetting = playerSpawnSetting;
        }

        void Start()
        {
            _cameraTransposer = _mainVirtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();

            PlayGameSequence(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid PlayGameSequence(CancellationToken token)
        {
            await PlayStartSequence(token);

            await PlayEndSequence(token);

            PlayResultSequence().Forget();

            return;
        }

        async UniTask PlayStartSequence(CancellationToken token)
        {
            await UniTask.Yield(token); // HUD, UIの初期化処理が終わってから(OnPlayerSpawnは良いがEnemyは複数いるためOnEnemySpawnがHUD, UIの初期化前に発行されたら意味がない)

            _enemySurface.BuildNavMesh();

            await SpawnPlayer(token);

            SetupTowers(token);

            _onBattleStart.OnNext(Unit.Default);
            
            this.UpdateAsObservable() // 更新処理開始
                .Subscribe(_ => _updater.FireUpdateStreams())
                .AddTo(this);
        }

        async UniTask SpawnPlayer(CancellationToken token)
        {
            var playerOwner = _playerFactory.Create(_playerRoot, _playerSpawnSetting.SpawnSetting.SpawnHex.transform.position) as IPlayerComponentCollection;

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
                .Subscribe(_ =>
                {
                    UpdateEnemyNavMesh();
                })
                .AddTo(this);

            _onPlayerSpawn.Value = playerOwner;
        }

        void SetupTowers(CancellationToken token)
        {
            GetComponentsInChildren(_towerList); // NonAlloc

            foreach(var tower in _towerList)
            {
                var enemySpawnObservable = tower.EnemySpawnObservable;
                enemySpawnObservable.EnemyList.ObserveAdd()
                    .Subscribe(addEvt =>
                    {
                        var enemyOwner = addEvt.Value;

                        CompositeDisposable enemyDisposables = new CompositeDisposable();
                        token.Register(() =>
                        {
                            enemyDisposables.Dispose();
                        });

                        // enemyがLiberateしたらNavMeshを再Bake
                        enemyOwner.LiberateObservable.SuccessLiberateHexList
                            .Subscribe(_ =>
                            {
                                UpdateEnemyNavMesh();
                            })
                            .AddTo(enemyDisposables);

                        var navMeshAgentController = enemyOwner.NavMeshAgentController;
                        navMeshAgentController.CurDestination
                            .Pairwise()
                            .Subscribe(pair =>
                            {
                                Hex prevDestination = pair.Previous, curDestination = pair.Current;

                                if (prevDestination != null) _enemyDestinationHexList.Remove(prevDestination);
                                if (curDestination != null) _enemyDestinationHexList.Add(curDestination);
                            })
                            .AddTo(enemyDisposables);
                        //! ↓Pairwiseでは初期値を取れないので、ActionStateController#Initで設定したCurDestination(LandedHex)がenemyDestinationHexListにAddされない分
                        _enemyDestinationHexList.Add(navMeshAgentController.CurDestination.Value);

                        enemyOwner.DieObservable.IsDead
                            .Where(isDead => isDead)
                            .Subscribe(_ =>
                            {
                                enemyDisposables.Dispose();
                            })
                            .AddTo(enemyDisposables);

                        _onEnemySpawn.OnNext(enemyOwner);
                        _enemyList.Add(enemyOwner);
                    })
                    .AddTo(this);
                enemySpawnObservable.EnemyList.ObserveRemove()
                    .Subscribe(removeEvt => _enemyList.Remove(removeEvt.Value))
                    .AddTo(this);

                tower.TowerController.Init();
                _onTowerInit.OnNext(tower);

                // TowerTypeが変わったらNavMeshを再Bake
                tower.TowerObservable.TowerType
                    .Skip(1)
                    .Subscribe(_ =>
                    {
                        UpdateEnemyNavMesh();
                    })
                    .AddTo(this);
            }
        }

        //TODO: FinishRule実装
        async UniTask PlayEndSequence(CancellationToken token)
        {
            _onPlayerSpawn.Value.DieObservable.OnFinishDie
                .First()
                .Subscribe(_ => _resultType = GameResultType.LOSE)
                .AddTo(this);

            await UniTask.WaitUntil(
                () => _resultType == GameResultType.LOSE || _towerList.All(tower => tower.TowerObservable.TowerType.Value == TowerType.PLAYER),
                cancellationToken: token);

            if (_resultType == GameResultType.NONE) _resultType = GameResultType.WIN;

            return;
        }

        async UniTask PlayResultSequence()
        {
            switch (_resultType)
            {
                case GameResultType.WIN: Debug.Log("WIN"); break;
                case GameResultType.LOSE: Debug.Log("LOSE"); break;
            }
        }

        void UpdateEnemyNavMesh()
        {
            _enemySurface.BuildNavMesh();
            _onUpdateNavMesh.OnNext(Unit.Default);
        }
    }
}
