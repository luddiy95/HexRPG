using System.Collections.Generic;
using System;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Stage;
    using Battle.Skill;

    public class PlayerSkillExecuter : ISkillController, ISkillObservable, IDisposable
    {
        IBattleObservable _battleObservable;
        ITransformController _transformController;
        IMemberObservable _memberObservable;
        ISelectSkillObservable _selectSkillObservable;
        ILiberateController _liberateController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IObservable<Unit> ISkillObservable.OnStartReservation => null;
        IObservable<Unit> ISkillObservable.OnFinishReservation => null;
        IObservable<IEnumerable<Hex>> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public PlayerSkillExecuter(
            IBattleObservable battleObservable,
            ITransformController transformController,
            IMemberObservable memberObservable,
            ISelectSkillObservable selectSkillObservable,
            ILiberateController liberateController
        )
        {
            _battleObservable = battleObservable;
            _transformController = transformController;
            _memberObservable = memberObservable;
            _selectSkillObservable = selectSkillObservable;
            _liberateController = liberateController;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex skillCenter, int skillRotation)
        {
            var runningSkill = 
                _memberObservable.CurMember.Value.SkillController.StartSkill(
                    index, 
                    _selectSkillObservable.SkillCenter,
                    _transformController.DefaultRotation + _selectSkillObservable.SelectedSkillRotation
                );

            _disposables.Clear();
            runningSkill.SkillObservable.OnSkillAttack // LiberateŒŸØ
                .Subscribe(attackRange =>
                {
                    var isExistAliveEnemyInAttackRange =
                        _battleObservable.EnemyList.Any(enemy => attackRange.Contains(enemy.TransformController.GetLandedHex()) && !enemy.DieObservable.IsDead.Value);
                    if(!isExistAliveEnemyInAttackRange) _liberateController.Liberate(attackRange);
                })
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _onFinishSkill.OnNext(Unit.Default);
                    _disposables.Clear();
                }).AddTo(_disposables);

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
