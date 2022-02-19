using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace HexRPG.Battle
{
    using Player;
    using Stage;

    //TODO: MonoBehaviourである必要はない(IInitializableで管理したい、これを一番最初に呼びたい)
    public class BattleManager : MonoBehaviour, IBattleObservable
    {
        IUpdater _updater;
        PlayerOwner.Factory _playerFactory;
        ISpawnSettings _spawnSettings;

        IObservable<IPlayerComponentCollection> IBattleObservable.OnPlayerSpawn => _onPlayerSpawn;
        readonly ISubject<IPlayerComponentCollection> _onPlayerSpawn = new Subject<IPlayerComponentCollection>();

        IObservable<ICustomComponentCollection> IBattleObservable.OnEnemySpawn => _onEnemySpawn;
        readonly ISubject<ICustomComponentCollection> _onEnemySpawn = new Subject<ICustomComponentCollection>();

        IObservable<Unit> IBattleObservable.OnBattleStart => _onBattleStart;
        ISubject<Unit> _onBattleStart = new Subject<Unit>();

        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        [Inject]
        public void Construct(
            IUpdater updater,
            PlayerOwner.Factory playerFactory, 
            ISpawnSettings spawnSettings)
        {
            _updater = updater;
            _playerFactory = playerFactory; 
            _spawnSettings = spawnSettings;
        }

        void Start()
        {
            PlayStartSequence(this.GetCancellationTokenOnDestroy()).Forget();
        }

        async UniTaskVoid PlayStartSequence(CancellationToken token)
        {
            await UniTask.Yield(token); // HUD, UIの初期化処理が終わってから

            var playerSpawnSetting = _spawnSettings.PlayerSpawnSetting;
            IPlayerComponentCollection player = _playerFactory.Create(null, playerSpawnSetting.SpawnHex.transform.position);

            var memberController = player.MemberController;
            await memberController.SpawnAllMember();
            memberController.ChangeMember(0);

            // Playerの位置を監視
            /*
            if (isPlayer && Owner.QueryInterface(out IUpdateObservable updateObservable))
            {
                updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                    .Subscribe(_ =>
                    {
                        _playerLandedHex = transformController.GetLandedHex();
                    })
                    .AddTo(this);
                _playerLandedHex = transformController.GetLandedHex();
            }
            */

            _onPlayerSpawn.OnNext(player);

            //TODO:
            /*
            var enemySpawnSetting = spawnSettings.EnemySpawnSettings;
            Array.ForEach(enemySpawnSetting, spawnSetting =>
            {
                var enemy = Spawn(spawnSetting.Prefab, spawnSetting.SpawnHex.transform.position, false);
                _onEnemySpawn.OnNext(enemy);
            });
            */

            _onBattleStart.OnNext(Unit.Default);

            // 更新処理開始
            this.UpdateAsObservable()
                .Subscribe(_ => _updater.FireUpdateStreams())
                .AddTo(this);

            //TODO: CancellationToken
        }
    }
}
