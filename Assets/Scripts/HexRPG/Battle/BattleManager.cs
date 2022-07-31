using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
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

    public enum GameResultType
    {
        NONE,
        WIN,
        LOSE
    }

    public class BattleManager : MonoBehaviour, IBattleObservable
    {
        BattleData _battleData;
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

        IObservable<ITowerComponentCollection> IBattleObservable.OnTowerInit => _onTowerInit;
        readonly ISubject<ITowerComponentCollection> _onTowerInit = new Subject<ITowerComponentCollection>();

        List<ITowerComponentCollection> IBattleObservable.TowerList => _towerList;
        List<ITowerComponentCollection> _towerList = new List<ITowerComponentCollection>(16);

        IObservable<Unit> IBattleObservable.OnUpdateNavMesh => _onUpdateNavMesh;
        readonly ISubject<Unit> _onUpdateNavMesh = new Subject<Unit>();

        [Header("�L�����p�X")]
        [SerializeField] GameObject _HUD;
        [SerializeField] GameObject _UI;
        [SerializeField] GameObject _sequenceUI;

        [Header("Sequence")]
        [SerializeField] GameObject _btnStart;
        [SerializeField] Text _resultText;

        [Header("�J����")]
        [SerializeField] CinemachineBrain _cinemachineBrain;
        [SerializeField] CinemachineVirtualCamera _mainVirtualCamera;
        [SerializeField] CinemachineTargetGroup _targetGroup;

        CinemachineBrain IBattleObservable.CinemachineBrain => _cinemachineBrain;
        CinemachineVirtualCamera IBattleObservable.MainVirtualCamera => _mainVirtualCamera;
        CinemachineOrbitalTransposer IBattleObservable.CameraTransposer => _cameraTransposer;
        CinemachineOrbitalTransposer _cameraTransposer;

        [Header("PlayerRoot")]
        [SerializeField] Transform _playerRoot;

        [Header("Enemy��NavMeshSurface")]
        [SerializeField] NavMeshSurface _enemySurface;

        GameResultType _resultType = GameResultType.NONE;

        [Inject]
        public void Construct(
            BattleData battleData,
            IUpdater updater,
            IUpdateObservable updateObservable,
            PlayerOwner.Factory playerFactory,
            IPlayerSpawnSetting playerSpawnSetting
        )
        {
            _battleData = battleData;
            _updater = updater;
            _updateObservable = updateObservable;
            _playerFactory = playerFactory;
            _playerSpawnSetting = playerSpawnSetting;
        }

        void Start()
        {
            _cameraTransposer = _mainVirtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();

            _HUD.SetActive(false);
            _UI.SetActive(false);
            _sequenceUI.SetActive(true);
            _btnStart.SetActive(true);
            _resultText.gameObject.SetActive(false);
        }

        public void StartGame()
        {
            _HUD.SetActive(true);
            _UI.SetActive(true);
            _sequenceUI.SetActive(false);
            PlayGameSequence(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid PlayGameSequence(CancellationToken token)
        {
            await PlayStartSequence(token);

            await PlayEndSequence(token);

            PlayResultSequence(token).Forget();

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

            foreach (var tower in _towerList)
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
                        //! ��Pairwise�ł͏����l�����Ȃ��̂ŁAActionStateController#Init�Őݒ肵��CurDestination(LandedHex)��enemyDestinationHexList��Add����Ȃ���
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

                // TowerType���ς������NavMesh����Bake
                tower.TowerObservable.TowerType
                    .Skip(1)
                    .Subscribe(_ =>
                    {
                        UpdateEnemyNavMesh();
                    })
                    .AddTo(this);
            }
        }

        async UniTask PlayEndSequence(CancellationToken token)
        {
            _onPlayerSpawn.Value.DieObservable.OnFinishDie
                .First()
                .Subscribe(_ => _resultType = GameResultType.LOSE)
                .AddTo(this);

            await UniTask.WaitUntil(
                () => _resultType == GameResultType.LOSE ||
                _towerList.All(tower => tower.EnemySpawnObservable.EnemyList.Count() == 0 && tower.TowerObservable.TowerType.Value == TowerType.PLAYER),
                cancellationToken: token);

            if (_resultType == GameResultType.NONE) _resultType = GameResultType.WIN;

            return;
        }

        async UniTask PlayResultSequence(CancellationToken token)
        {
            await UniTask.Delay(2000, cancellationToken: token);

            //TODO: ���ʕ\��
            _sequenceUI.SetActive(true);
            _btnStart.SetActive(false);
            _resultText.gameObject.SetActive(true);
            switch (_resultType)
            {
                case GameResultType.WIN: _resultText.text = "WIN!!"; break;
                case GameResultType.LOSE: _resultText.text = "LOSE..."; break;
            }

            await UniTask.Delay(2000, cancellationToken: token);

            _battleData.result = _resultType;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void UpdateEnemyNavMesh()
        {
            _enemySurface.BuildNavMesh();
            _onUpdateNavMesh.OnNext(Unit.Default);
        }
    }
}
