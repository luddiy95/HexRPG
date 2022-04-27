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
        readonly ISubject<Unit> _onBattleStart = new Subject<Unit>();

        IPlayerComponentCollection _playerOwner;
        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        List<IEnemyComponentCollection> IBattleObservable.EnemyList => _enemyList;
        List<IEnemyComponentCollection> _enemyList;

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
            await UniTask.Yield(token); // HUD, UI�̏������������I����Ă���(OnPlayerSpawn�͗ǂ���Enemy�͕������邽��OnEnemySpawn��HUD, UI�̏������O�ɔ��s���ꂽ��Ӗ����Ȃ�)

            await SpawnPlayer(token);

            await SpawnEnemies(token);

            _onBattleStart.OnNext(Unit.Default);
            
            this.UpdateAsObservable() // �X�V�����J�n
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

            //TODO: PlayerAnimationBehaviour������(�eMember��AnimatorBehaviour������/Combat, Skill�����������Ă��Ȃ���΂Ȃ�Ȃ�)
            //TODO: -> PlayerActionStateController�X�^�[�g(?)(PlayerAnimationBehaviour�����������Ă��Ȃ��ƃ��[�V�������Đ��ł��Ȃ�)

            _targetGroup.m_Targets[0].target = _playerOwner.TransformController.MoveTransform;

            // Player�̈ʒu���Ď�
            _playerLandedHex = _playerOwner.TransformController.GetLandedHex();
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _playerLandedHex = _playerOwner.TransformController.GetLandedHex();
                })
                .AddTo(this);

            _onPlayerSpawn.Value = _playerOwner;
        }

        async UniTask SpawnEnemies(CancellationToken token)
        {
            _enemyList = _spawnSettings.EnemySpawnSettings
                .Select((setting, index) => _enemyFactories[index].Create(_enemyRoot, setting.SpawnHex.transform.position) as IEnemyComponentCollection).ToList();

            await UniTask.WaitUntil(() => _enemyList.All(enemy => enemy.SkillSpawnObservable.IsAllSkillSpawned), cancellationToken: token);

            _enemyList.ForEach(enemy => enemy.AnimationController.Init());

            _enemyList.ForEach(enemy => _onEnemySpawn.OnNext(enemy));
        }
    }
}
