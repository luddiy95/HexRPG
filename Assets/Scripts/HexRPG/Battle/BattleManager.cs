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
        /// �eEnemy�́u���������Ƃ��Ă���ړI�n or hex�̐^�񒆂ŐÎ~���Ă����Ԃ�LandedHex�v
        /// </summary>
        IEnumerable<Hex> IBattleObservable.EnemyDestinationHexList => _enemyDestinationHexList;
        readonly List<Hex> _enemyDestinationHexList = new List<Hex>(32);

        List<ITowerComponentCollection> _towers = new List<ITowerComponentCollection>(16);

        CinemachineBrain IBattleObservable.CinemachineBrain => _cinemachineBrain;
        [SerializeField] CinemachineBrain _cinemachineBrain;

        CinemachineVirtualCamera IBattleObservable.MainVirtualCamera => _mainVirtualCamera;
        [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;

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
            await UniTask.Yield(token); // HUD, UI�̏������������I����Ă���(OnPlayerSpawn�͗ǂ���Enemy�͕������邽��OnEnemySpawn��HUD, UI�̏������O�ɔ��s���ꂽ��Ӗ����Ȃ�)

            _enemySurface.BuildNavMesh();

            await SpawnPlayer(token);

            SetupTowers(token);

            _onBattleStart.OnNext(Unit.Default);
            
            this.UpdateAsObservable() // �X�V�����J�n
                .Subscribe(_ => _updater.FireUpdateStreams())
                .AddTo(this);
        }

        async UniTask SpawnPlayer(CancellationToken token)
        {
            var playerOwner = _playerFactory.Create(_playerRoot, _playerSpawnSetting.SpawnSetting.SpawnHex.transform.position) as IPlayerComponentCollection;

            var memberController = playerOwner.MemberController;
            await memberController.SpawnAllMember(token);
            memberController.ChangeMember(0); //! �����ł悤�₭CurMember�����s�����

            playerOwner.CharacterActionStateController.Init(); // ���X�̏��������I����Ă���ActionStateController��������

            _targetGroup.m_Targets[0].target = playerOwner.TransformController.MoveTransform;
            // Player�̈ʒu���Ď�
            _playerLandedHex = playerOwner.TransformController.GetLandedHex();
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _playerLandedHex = playerOwner.TransformController.GetLandedHex();
                })
                .AddTo(this);
            // Player��Liberate������NavMesh����Bake
            playerOwner.LiberateObservable.SuccessLiberateHexList
                .Subscribe(_ => _enemySurface.BuildNavMesh())
                .AddTo(this);

            _onPlayerSpawn.Value = playerOwner;
        }

        void SetupTowers(CancellationToken token)
        {
            GetComponentsInChildren(_towers); // NonAlloc

            foreach(var tower in _towers)
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

                        // enemy��Liberate������NavMesh����Bake
                        enemyOwner.LiberateObservable.SuccessLiberateHexList
                            .Subscribe(_ => _enemySurface.BuildNavMesh())
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
            }
        }

        //TODO: FinishRule����
        async UniTask PlayEndSequence(CancellationToken token)
        {
            _onPlayerSpawn.Value.DieObservable.OnFinishDie
                .First()
                .Subscribe(_ => _resultType = GameResultType.LOSE)
                .AddTo(this);

            await UniTask.WaitUntil(
                () => _resultType == GameResultType.LOSE || _towers.All(tower => tower.TowerObservable.TowerType.Value == TowerType.PLAYER),
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
    }
}
