using System;
using System.Linq;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Player
{
    public class PlayerDieObservable : IDieObservable, IInitializable, IDisposable
    {
        IMemberObservable _memberObservable;
        IMemberController _memberController;
        ILocomotionController _locomotionController;

        IReadOnlyReactiveProperty<bool> IDieObservable.IsDead => null;

        IObservable<Unit> IDieObservable.OnFinishDie => _onFinishDie;
        readonly ISubject<Unit> _onFinishDie = new Subject<Unit>();

        IDisposable _memberChangeDisposable;
        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerDieObservable(
            IMemberObservable memberObservable,
            IMemberController memberController,
            ILocomotionController locomotionController
        )
        {
            _memberObservable = memberObservable;
            _memberController = memberController;
            _locomotionController = locomotionController;
        }

        void IInitializable.Initialize()
        {
            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(curMember =>
                {
                    _memberChangeDisposable?.Dispose();
                    _memberChangeDisposable = curMember.DieObservable.OnFinishDie
                        .Subscribe(_ =>
                        {
                            _memberChangeDisposable.Dispose();

                            var memberList = _memberObservable.MemberList;
                            var changeableMember = memberList.FirstOrDefault(member => member.DieObservable.IsDead.Value == false);
                            if (changeableMember == null)
                            {
                                _onFinishDie.OnNext(Unit.Default);
                                _onFinishDie.OnCompleted();
                                return;
                            }
                            _memberController.ChangeMember(memberList.IndexOf(changeableMember));
                            _locomotionController.ForceRotate(0);
                        });
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _memberChangeDisposable.Dispose();
            _disposables.Dispose();
        }
    }
}
