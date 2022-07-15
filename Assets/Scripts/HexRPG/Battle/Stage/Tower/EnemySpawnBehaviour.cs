using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

namespace HexRPG.Battle.Stage.Tower
{
    using Enemy;

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

        IReadOnlyReactiveCollection<IEnemyComponentCollection> IEnemySpawnObservable.EnemyList => _enemyList;
        readonly ReactiveCollection<IEnemyComponentCollection> _enemyList = new ReactiveCollection<IEnemyComponentCollection>();

        CancellationTokenSource _spawnCts = null;

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
                            StopSpawn(); break;
                        case TowerType.ENEMY:
                            SpawnEnemies(); break;
                    }
                })
                .AddTo(this);
        }

        void SpawnEnemies()
        {
            // Dynamic Enemy
            _spawnCts = new CancellationTokenSource();
            var dynamicEnemySpawnSettings = _enemySpawnSettings.DynamicEnemySpawnSettings;
            for (int i = 0; i < dynamicEnemySpawnSettings.Count; i++)
            {
                StartDynamicEnemySpawnSequence(_enemyFactories[i], dynamicEnemySpawnSettings[i], _spawnCts.Token).Forget();
            }

            // Static Enemy
            var staticEnemySpawnSettings = _enemySpawnSettings.StaticEnemySpawnSettings;
            for (int i = 0; i < staticEnemySpawnSettings.Count; i++)
            {
                SpawnEnemy(_enemyFactories[dynamicEnemySpawnSettings.Count + i], staticEnemySpawnSettings[i].SpawnHex, _spawnCts.Token).Forget();
            }
        }

        async UniTaskVoid StartDynamicEnemySpawnSequence(EnemyOwner.Factory factory, DynamicSpawnSetting spawnSetting, CancellationToken token)
        {
            await UniTask.Delay(spawnSetting.FirstSpawnInterval, cancellationToken: token);

            // enemyNameを取得するための最初の一体
            Hex spawnHex = null;
            try
            {
                spawnHex = _towerObservable.FixedHexList.First(hex => _enemyList.Any(enemy => enemy.TransformController.GetLandedHex() == hex) == false);
            }
            catch (System.NullReferenceException e)
            {
                throw e;
            }
            var enemy = await SpawnEnemy(factory, spawnHex, token);
            var enemyName = enemy.ProfileSetting.Name;

            CancellationTokenSource spawnIteraterCts = null;
            void CancelIterater()
            {
                spawnIteraterCts?.Cancel();
                spawnIteraterCts?.Dispose();
                spawnIteraterCts = null;
            }

            token.Register(() => CancelIterater());

            while (true)
            {
                spawnIteraterCts = new CancellationTokenSource();

                StartDynamicEnemySpawnIterater(factory, spawnSetting, spawnIteraterCts.Token).Forget();

                // 最大数になるまでawait
                await UniTask.WaitUntil(() => _enemyList.Count(enemy => enemy.ProfileSetting.Name == enemyName) >= spawnSetting.MaxCount, cancellationToken: token);

                CancelIterater();

                // 最大数未満になるまでawait
                await UniTask.WaitUntil(() =>
                    _enemyList.Count(enemy => enemy.ProfileSetting.Name == enemyName) < spawnSetting.MaxCount,
                    cancellationToken: token);
            }
        }

        async UniTaskVoid StartDynamicEnemySpawnIterater(EnemyOwner.Factory factory, DynamicSpawnSetting spawnSetting, CancellationToken token)
        {
            while (true)
            {
                await UniTask.Delay(spawnSetting.SpawnInterval, cancellationToken: token);
                Hex spawnHex = null;
                try
                {
                    spawnHex = _towerObservable.FixedHexList.First(hex => _enemyList.Any(enemy => enemy.TransformController.GetLandedHex() == hex) == false);
                }
                catch (System.NullReferenceException e)
                {
                    throw e;
                }
                SpawnEnemy(factory, spawnHex, token).Forget();
            }
        }

        async UniTask<IEnemyComponentCollection> SpawnEnemy(EnemyOwner.Factory factory, Hex spawnHex, CancellationToken token)
        {
            var enemy = factory.Create(_enemyRoot, spawnHex.transform.position);
            var enemyOwner = enemy as IEnemyComponentCollection;

            enemyOwner.Health.Init();

            await UniTask.WaitUntil(() => enemyOwner.CombatSpawnObservable.IsCombatSpawned, cancellationToken: token);
            await UniTask.WaitUntil(() => enemyOwner.SkillSpawnObservable.IsAllSkillSpawned, cancellationToken: token);

            enemyOwner.AnimationController.Init();
            enemyOwner.CharacterActionStateController.Init(); // 諸々の初期化が終わってからActionStateControllerを初期化した方が良い

            //TODO: 1F遅らせても一瞬Dieモーションが見えている？
            await UniTask.DelayFrame(1, cancellationToken: token);
            enemyOwner.DieController.Init();
            enemyOwner.ActiveController.SetActive(true);

            enemyOwner.DieObservable.OnFinishDie
                .First()
                .Subscribe(_ =>
                {
                    enemy.Dispose();
                    _enemyList.Remove(enemy);
                })
                .AddTo(this);

            _enemyList.Add(enemy);

            return enemy;
        }

        void StopSpawn()
        {
            CancelSpawnSequence();
            foreach (var enemy in _enemyList)
            {
                if (_towerObservable.FixedHexList.Contains(enemy.TransformController.GetLandedHex()))
                {
                    enemy.Health.ForceDie(); //TODO: Die中のEnemyも再度Dieしてしまうのでは
                }
            }
        }

        void CancelSpawnSequence()
        {
            _spawnCts?.Cancel();
            _spawnCts?.Dispose();
            _spawnCts = null;
        }

        void OnDestroy()
        {
            CancelSpawnSequence();
        }
    }
}
