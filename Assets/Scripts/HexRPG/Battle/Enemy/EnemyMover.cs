using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    using Stage;

    public class EnemyMover : IMoveController, IDisposable
    {
        IBattleObservable _battleObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyMover(
            IBattleObservable battleObservable)
        {
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
