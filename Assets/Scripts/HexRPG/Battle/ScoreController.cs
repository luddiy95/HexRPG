using System;
using System.Linq;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public enum ScoreType
    {
        LIBERATE, // HexÇLiberateÇµÇΩ

        DEFEAT_MULTI_ENEMY, // ï°êîÇÃEnemyÇìØéûÇ…ì|ÇµÇΩ

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

    public class ScoreData
    {
        public ScoreInfo scoreInfo;
        public int count;
    }

    public interface IScoreController
    {
        void AquireScore(ScoreType type, int aquireCount);
    }

    public interface IScoreObservable
    {
        IReadOnlyReactiveProperty<int> CurScore { get; }
        IObservable<ScoreData> OnAddScoreData { get; }
    }

    public class ScoreController : IScoreController, IScoreObservable, IInitializable
    {
        BattleData _battleData;

        IReadOnlyReactiveProperty<int> IScoreObservable.CurScore => _curScore;
        readonly IReactiveProperty<int> _curScore = new ReactiveProperty<int>();

        IObservable<ScoreData> IScoreObservable.OnAddScoreData => _onAddScoreData;
        readonly ISubject<ScoreData> _onAddScoreData = new Subject<ScoreData>();

        public ScoreController(
            BattleData battleData
        )
        {
            _battleData = battleData;
        }

        void IInitializable.Initialize()
        {
            _curScore.Value = _battleData.InitScore;
        }

        void IScoreController.AquireScore(ScoreType type, int aquireCount)
        {
            var scoreInfo = _battleData.ScoreInfoMap.FirstOrDefault(data => data.type == type);
            if (scoreInfo == null) return;

            _curScore.Value += scoreInfo.score * aquireCount;
            _onAddScoreData.OnNext(new ScoreData() { scoreInfo = scoreInfo, count = aquireCount });
        }
    }
}
