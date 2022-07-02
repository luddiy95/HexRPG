using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    using Stage;

    public interface IEnemySpawnObservable
    {
        IReadOnlyReactiveCollection<IEnemyComponentCollection> EnemyList { get; }
    }

    public class EnemySpawnBehaviour : MonoBehaviour, IEnemySpawnObservable
    {
        List<EnemyOwner.Factory> _enemyFactories;
        IEnemySpawnSettings _enemySpawnSettings;
        ITowerObservable _towerObservable;

        [SerializeField] Transform _enemyRoot;
        [SerializeField] Hex _rootHex;

        IReadOnlyReactiveCollection<IEnemyComponentCollection> IEnemySpawnObservable.EnemyList => _enemyList;
        readonly IReactiveCollection<IEnemyComponentCollection> _enemyList = new ReactiveCollection<IEnemyComponentCollection>();

        CancellationTokenSource _spawnEnemyCancellationTokenSource = new CancellationTokenSource();

        [Inject]
        public void Construct(
            List<EnemyOwner.Factory> enemyFactories,
            IEnemySpawnSettings enemySpawnSettings,
            ITowerObservable towerObservable
        )
        {
            _enemyFactories = enemyFactories;
            _enemySpawnSettings = enemySpawnSettings;
            _towerObservable = towerObservable;
        }

        void Start()
        {
            _towerObservable.TowerType
                .Skip(1)
                .Subscribe(type =>
                {
                    switch (type)
                    {
                        case TowerType.PLAYER:
                            TokenCancel(); break;
                        case TowerType.ENEMY:
                            _spawnEnemyCancellationTokenSource = new CancellationTokenSource();
                            SpawnEnemies(_spawnEnemyCancellationTokenSource.Token).Forget(); break;
                    }
                })
                .AddTo(this);
        }

        async UniTask SpawnEnemies(CancellationToken token)
        {
            // Dynamic Enemy
            var dynamicEnemySpawnSettings = _enemySpawnSettings.DynamicEnemySpawnSettings;
            for (int i = 0; i < dynamicEnemySpawnSettings.Length; i++)
            {
                StartDynamicEnemySpawner(_enemyFactories[i], dynamicEnemySpawnSettings[i], token).Forget();
            }

            // Static Enemy
            var staticEnemySpawnSettings = _enemySpawnSettings.StaticEnemySpawnSettings;
            for (int i = 0; i < staticEnemySpawnSettings.Length - 1; i++)
            {
                await SpawnEnemy(_enemyFactories[dynamicEnemySpawnSettings.Length + i], staticEnemySpawnSettings[i].SpawnHex, token);
            }
        }

        async UniTaskVoid StartDynamicEnemySpawner(EnemyOwner.Factory factory, DynamicSpawnSetting spawnSetting, CancellationToken token)
        {
            await UniTask.Delay(spawnSetting.FirstSpawnInterval * 1000, cancellationToken: token);

            // enemyNameÇéÊìæÇ∑ÇÈÇΩÇﬂÇÃç≈èâÇÃàÍëÃ
            var enemy = await SpawnEnemy(factory, _rootHex, token);
            var enemyName = enemy.ProfileSetting.Name;

            while (true)
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                StartDynamicEnemySpawnIterater(factory, spawnSetting, cancellationTokenSource.Token).Forget();

                // ç≈ëÂêîÇ…Ç»ÇÈÇ‹Ç≈await
                await UniTask.WaitUntil(() =>
                    _enemyList.Count(enemy => enemy.ProfileSetting.Name == enemyName) >= spawnSetting.MaxCount,
                    cancellationToken: token);

                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();

                // ç≈ëÂêîñ¢ñûÇ…Ç»ÇÈÇ‹Ç≈await
                await UniTask.WaitUntil(() =>
                    _enemyList.Count(enemy => enemy.ProfileSetting.Name == enemyName) < spawnSetting.MaxCount,
                    cancellationToken: token);
            }
        }

        async UniTaskVoid StartDynamicEnemySpawnIterater(EnemyOwner.Factory factory, DynamicSpawnSetting spawnSetting, CancellationToken token)
        {
            while (true)
            {
                await UniTask.Delay(spawnSetting.SpawnInterval * 1000, cancellationToken: token);
                await SpawnEnemy(factory, _rootHex, token);
            }
        }

        async UniTask<IEnemyComponentCollection> SpawnEnemy(EnemyOwner.Factory factory, Hex spawnHex, CancellationToken token)
        {
            var enemy = factory.Create(_enemyRoot, spawnHex.transform.position);
            IEnemyComponentCollection enemyOwner = enemy;

            CompositeDisposable enemyDisposables = new CompositeDisposable();
            var dieObservable = enemyOwner.DieObservable;
            dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ => _enemyList.Remove(enemyOwner))
                .AddTo(enemyDisposables);
            enemyOwner.DieObservable.OnFinishDie
                .Subscribe(_ =>
                {
                    Destroy(enemy.gameObject);
                    enemyDisposables.Dispose();
                })
                .AddTo(enemyDisposables);

            await UniTask.WaitUntil(() => enemyOwner.CombatSpawnObservable.IsCombatSpawned, cancellationToken: token);
            await UniTask.WaitUntil(() => enemyOwner.SkillSpawnObservable.IsAllSkillSpawned, cancellationToken: token);

            enemyOwner.AnimationController.Init();
            enemyOwner.CharacterActionStateController.Init(); // èîÅXÇÃèâä˙âªÇ™èIÇÌÇ¡ÇƒÇ©ÇÁActionStateControllerÇèâä˙âªÇµÇΩï˚Ç™ó«Ç¢

            enemyOwner.ActiveController.SetActive(true);

            _enemyList.Add(enemy);

            return enemyOwner;
        }

        void TokenCancel()
        {
            _spawnEnemyCancellationTokenSource?.Cancel();
            _spawnEnemyCancellationTokenSource?.Dispose();
            _spawnEnemyCancellationTokenSource = null;
        }

        void OnDestroy()
        {
            TokenCancel();
        }
    }
}
