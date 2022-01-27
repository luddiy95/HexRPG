using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle
{
    using Stage;

    public class BattleManager : AbstractCustomComponentBehaviour, IBattleObservable
    {
        [Header("直おきCustomComponentCollection")]
        [SerializeField] GameObject[] _instances;

        IComponentCollectionFactory _factory = null;

        IObservable<ICustomComponentCollection> IBattleObservable.OnPlayerSpawn => _onPlayerSpawn;
        readonly ISubject<ICustomComponentCollection> _onPlayerSpawn = new Subject<ICustomComponentCollection>();

        IObservable<ICustomComponentCollection> IBattleObservable.OnEnemySpawn => _onEnemySpawn;
        readonly ISubject<ICustomComponentCollection> _onEnemySpawn = new Subject<ICustomComponentCollection>();

        IObservable<Unit> IBattleObservable.OnBattleStart => _onBattleStart;
        ISubject<Unit> _onBattleStart = new Subject<Unit>();

        Hex IBattleObservable.PlayerLandedHex => _playerLandedHex;
        Hex _playerLandedHex = null;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IBattleObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.QueryInterface(out _factory))
            {
                Main(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        async UniTaskVoid Main(CancellationToken token)
        {
            await UniTask.Yield(token);

            // 直置きGameObjectをCustomComponentCollectionにする
            foreach (var instance in _instances)
            {
                _factory.CreateComponentCollectionWithoutInstantiate(instance, null, null);
            }

            if(Owner.QueryInterface(out ISpawnSettings spawnSettings))
            {
                var playerSpawnSetting = spawnSettings.PlayerSpawnSetting;
                var player = Spawn(playerSpawnSetting.Prefab, playerSpawnSetting.SpawnHex.transform.position, true);
                _onPlayerSpawn.OnNext(player);

                var enemySpawnSetting = spawnSettings.EnemySpawnSettings;
                Array.ForEach(enemySpawnSetting, spawnSetting =>
                {
                    var enemy = Spawn(spawnSetting.Prefab, spawnSetting.SpawnHex.transform.position, false);
                    _onEnemySpawn.OnNext(enemy);
                });
            }

            // 各CustomComponentCollectionのCreateが済んだらBattleStart
            _onBattleStart.OnNext(Unit.Default);
        }

        ICustomComponentCollection Spawn(GameObject prefab, Vector3 spawnPos, bool isPlayer)
        {
            var components = new List<ICustomComponent>
            {
                new ActionStateController(),
            };

            if (isPlayer)
            {
                components.AddRange(new List<ICustomComponent>
                {

                });
            }
            else
            {
                components.AddRange(new List<ICustomComponent>
                {
                    new Health()
                });
            }

            // キャラクタ生成
            var obj = _factory.CreateComponentCollection(prefab, components, owner =>
            {
                if (isPlayer && Owner.QueryInterface(out ICharacterInput input))
                {
                    owner.RegisterInterface(input);
                }
            });

            // 出現位置
            if(obj.QueryInterface(out ITransformController transformController))
            {
                transformController.Position = spawnPos;

                // Playerの位置を監視
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
            }

            return obj;
        }
    }
}
