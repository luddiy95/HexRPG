using UnityEngine;
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
        IAttackController _attackController;
        IAttackObservable _attackObservable;
        ILiberateController _liberateController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IObservable<Unit> ISkillObservable.OnStartReservation => null;
        IObservable<Unit> ISkillObservable.OnFinishReservation => null;
        IObservable<SkillAttackSetting> ISkillObservable.OnSkillAttackEnable => null;
        IObservable<Unit> ISkillObservable.OnSkillAttackDisable => null;
        IObservable<Hex[]> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public PlayerSkillExecuter(
            IBattleObservable battleObservable,
            ITransformController transformController,
            IMemberObservable memberObservable,
            ISelectSkillObservable selectSkillObservable,
            IAttackController attackController,
            IAttackObservable attackObservable,
            ILiberateController liberateController
        )
        {
            _battleObservable = battleObservable;
            _transformController = transformController;
            _memberObservable = memberObservable;
            _selectSkillObservable = selectSkillObservable;
            _attackController = attackController;
            _attackObservable = attackObservable;
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
            runningSkill.SkillObservable.OnSkillAttackEnable
                .Subscribe(attackSetting =>
                {
                    attackSetting.attribute = runningSkill.SkillSetting.Attribute;

                    _attackController.StartAttack(attackSetting);
                })
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnSkillAttackDisable
                .Subscribe(_ => _attackController.FinishAttack())
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnSkillAttack // Liberate����
                .Subscribe(attackRange =>
                {
                    var isExistAliveEnemyInAttackRange =
                        _battleObservable.EnemyList.Any(enemy =>
                            attackRange.Contains(enemy.TransformController.GetLandedHex()) && !enemy.DieObservable.IsDead.Value);
                    if(!isExistAliveEnemyInAttackRange) _liberateController.Liberate(attackRange);
                })
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _onFinishSkill.OnNext(Unit.Default);
                }).AddTo(_disposables);

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
