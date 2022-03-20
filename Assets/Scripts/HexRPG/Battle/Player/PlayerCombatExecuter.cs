using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Combat;
    using Stage;

    public class PlayerCombatController : ICombatController, ICombatObservable
    {
        IMemberObservable _memberObservable;
        ITransformController _transformController;
        IStageController _stageController;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection _runningCombat;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerCombatController(
            IMemberObservable memberObservable,
            ITransformController transformController,
            IStageController stageController
        )
        {
            _memberObservable = memberObservable;
            _transformController = transformController;
            _stageController = stageController;
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
                        _onFinishCombat.OnNext(Unit.Default);
                        _runningCombat = null;
                    })
                    .AddTo(_disposables);
            }

            return _runningCombat;
        }
    }
}
