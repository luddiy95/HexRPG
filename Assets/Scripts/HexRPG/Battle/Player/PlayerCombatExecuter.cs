using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Combat;

    public class PlayerCombatExecuter : ICombatController, ICombatObservable, IDisposable
    {
        IMemberObservable _memberObservable;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection _runningCombat;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerCombatExecuter(
            IMemberObservable memberObservable
        )
        {
            _memberObservable = memberObservable;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            var combatController = _memberObservable.CurMember.Value.CombatController;
            if (_runningCombat != null)
            {
                // ƒRƒ“ƒ{“ü—Í
                combatController.Combat();
            }
            else
            {
                _runningCombat = combatController.Combat();

                _disposables.Clear();
                // I—¹ˆ—
                _runningCombat.CombatObservable.OnFinishCombat
                    .Subscribe(_ =>
                    {
                        _runningCombat = null;

                        _onFinishCombat.OnNext(Unit.Default);

                        _disposables.Clear();
                    })
                    .AddTo(_disposables);
            }

            return _runningCombat;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
