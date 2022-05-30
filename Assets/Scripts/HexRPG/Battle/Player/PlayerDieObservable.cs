using System;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Player
{
    public class PlayerDieObservable : IDieObservable, IInitializable, IDisposable
    {
        IMemberObservable _memberObservable;

        IReadOnlyReactiveProperty<bool> IDieObservable.IsDead => _isDead;
        readonly IReactiveProperty<bool> _isDead = new ReactiveProperty<bool>(false);

        IObservable<Unit> IDieObservable.OnFinishDie => null;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerDieObservable(
            IMemberObservable memberObservable
        )
        {
            _memberObservable = memberObservable;
        }

        void IInitializable.Initialize()
        {
            _memberObservable.MemberList.ObserveCountChanged()
                .Where(count => count == 0)
                .Subscribe(_ =>
                {
                    _isDead.Value = true;
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
