using System;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Player
{
    public class PlayerDieObservable : IDieObservable, IInitializable, IDisposable
    {
        IMemberObservable _memberObservable;

        IReadOnlyReactiveProperty<bool> IDieObservable.IsDead => null;

        IObservable<Unit> IDieObservable.OnFinishDie => _onFinishDie;
        readonly ISubject<Unit> _onFinishDie = new Subject<Unit>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerDieObservable(
            IMemberObservable memberObservable
        )
        {
            _memberObservable = memberObservable;
        }

        void IInitializable.Initialize()
        {
            _memberObservable.OnAllMemberDead
                .Subscribe(_ =>
                {
                    _onFinishDie.OnNext(Unit.Default);
                    _onFinishDie.OnCompleted();
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
