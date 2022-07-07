using System;
using System.Linq;
using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public enum ScoreType
    {
        LIBERATE, // HexをLiberateした

        DEFEAT_MULTI_ENEMY, // 複数のEnemyを同時に倒した

        WEAK_ATTACK,
        CRITICAL_ATTACK
    }

    [Serializable]
    public class ScoreInfo
    {
        public ScoreType type;
        public int score;
        public string message;
    }

    public struct ScoreData
    {
        public ScoreInfo scoreInfo;
        public int count;
    }

    //TODO: Inspectorでしか使わない？
    public interface IScoreController
    {
        void AcquireScore(ScoreType type, int acquireCount);
    }

    public interface IScoreObservable
    {
        IReadOnlyReactiveProperty<int> CurScore { get; }
        int ScoreMax { get; }

        IObservable<ScoreData> OnAddScoreData { get; }
    }

    public class ScoreController : IScoreController, IScoreObservable, IInitializable, IDisposable
    {
        BattleData _battleData;
        IBattleObservable _battleObservable;

        IReadOnlyReactiveProperty<int> IScoreObservable.CurScore => _curScore;
        readonly IReactiveProperty<int> _curScore = new ReactiveProperty<int>();

        int IScoreObservable.ScoreMax => _scoreMax;
        int _scoreMax = 0;

        IObservable<ScoreData> IScoreObservable.OnAddScoreData => _onAddScoreData;
        readonly ISubject<ScoreData> _onAddScoreData = new Subject<ScoreData>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public ScoreController(
            BattleData battleData,
            IBattleObservable battleObservable
        )
        {
            _battleData = battleData;
            _battleObservable = battleObservable;
        }

        void IInitializable.Initialize()
        {
            _scoreMax = _battleData.scoreMax;
            _curScore.Value = _battleData.initScore;

            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    // Liberate成功
                    playerOwner.LiberateObservable.SuccessLiberateHexList
                        .Where(hexList => hexList.Length > 0)
                        .Subscribe(hexList => AcquireScore(ScoreType.LIBERATE, hexList.Length))
                        .AddTo(_disposables);

                    // Enemyへ弱点攻撃/クリティカル攻撃
                    playerOwner.AttackObservable.OnAttackHit
                        .Subscribe(hitData =>
                        {
                            switch (hitData.HitType)
                            {
                                case HitType.WEAK: AcquireScore(ScoreType.WEAK_ATTACK, 1); break;
                                case HitType.CRITICAL: AcquireScore(ScoreType.CRITICAL_ATTACK, 1); break;
                            }
                        })
                        .AddTo(_disposables);
                })
                .AddTo(_disposables);
        }

        void IScoreController.AcquireScore(ScoreType type, int acquireCount)
        {
            AcquireScore(type, acquireCount);
        }

        void AcquireScore(ScoreType type, int acquireCount)
        {
            var scoreInfo = _battleData.scoreInfoMap.FirstOrDefault(data => data.type == type);
            if (scoreInfo == null) return;

            var score = _curScore.Value + scoreInfo.score * acquireCount;
            _curScore.Value = Mathf.Min(score, _scoreMax);
            _onAddScoreData.OnNext(new ScoreData() { scoreInfo = scoreInfo, count = acquireCount });
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
