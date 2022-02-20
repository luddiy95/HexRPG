using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    using Stage;

    public class EnemyMover : IMoveController, IDisposable
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

        void IMoveController.StartMove(Hex destinationHex)
        {

        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
