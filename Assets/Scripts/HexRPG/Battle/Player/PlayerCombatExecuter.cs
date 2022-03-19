using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Combat;

    public class PlayerCombatController : ICombatController, ICombatObservable
    {
        IMemberObservable _memberObservable;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection _runningCombat;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerCombatController(
            IMemberObservable memberObservable
        )
        {
            _memberObservable = memberObservable;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            //TODO: Playerを仲介する必要があるのか(Skillは必要だけど。。)
            var combatController = _memberObservable.CurMember.Value.CombatController;
            if (_runningCombat != null)
            {
                combatController.Combat();
            }
            else
            {
                _runningCombat = combatController.Combat();

                _disposables.Clear();
                _runningCombat.CombatObservable.OnFinishCombat
                    .Subscribe(_ =>
                    {

                    })
                    .AddTo(_disposables);
            }

            return _runningCombat;
        }
    }
}
