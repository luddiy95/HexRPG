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

        IReadOnlyReactiveProperty<bool> IDieObservable.IsDead => null;

        IObservable<Unit> IDieObservable.OnFinishDie => _onFinishDie;
        readonly ISubject<Unit> _onFinishDie = new Subject<Unit>();

        IDisposable _memberChangeDisposable;
        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerDieObservable(
            IMemberObservable memberObservable,
            IMemberController memberController
        )
        {
            _memberObservable = memberObservable;
            _memberController = memberController;
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
                            _memberChangeDisposable?.Dispose();

                            curMember.SkillSpawnObservable.SkillList.ForEach(skill => skill.Skill.HideEffect());

                            var memberList = _memberObservable.MemberList;
                            var changeableMember = memberList.FirstOrDefault(member => member.DieObservable.IsDead.Value == false);
                            if (changeableMember == null)
                            {
                                _onFinishDie.OnNext(Unit.Default);
                                return;
                            }
                            _memberController.ChangeMember(memberList.IndexOf(changeableMember));
                        });
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _memberChangeDisposable?.Dispose();
            _disposables.Dispose();
        }
    }
}
