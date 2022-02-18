using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    using Stage;

    public class EnemyMover : IMover, IInitializable, IDisposable
    {
        ITurnToTarget _turnToTarget;
        IBattleObservable _battleObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyMover(
            ITurnToTarget turnToTarget,
            IBattleObservable battleObservable)
        {
            _turnToTarget = turnToTarget;
            _battleObservable = battleObservable;
        }

        void IInitializable.Initialize()
        {
            _battleObservable.OnBattleStart
                .First()
                .Subscribe(_ => _turnToTarget.TurnToTarget())
                .AddTo(_disposables);
        }

        void IMover.StartMove(Hex destinationHex)
        {

        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
