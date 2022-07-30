using System.Collections.Generic;
using System;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Enemy;
    using Stage;
    using Battle.Skill;

    public class PlayerSkillExecuter : ISkillController, ISkillObservable, IDisposable
    {
        IBattleObservable _battleObservable;
        IScoreController _scoreController;
        ITransformController _transformController;
        IMemberObservable _memberObservable;
        ISelectSkillObservable _selectSkillObservable;
        ILiberateController _liberateController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IObservable<Unit> ISkillObservable.OnStartReservation => null;
        IObservable<Unit> ISkillObservable.OnFinishReservation => null;
        IObservable<IEnumerable<Hex>> ISkillObservable.OnAttackEnable => null;
        IObservable<HitData> ISkillObservable.OnAttackHit => null;
        IObservable<IEnumerable<Hex>> ISkillObservable.OnAttackDisable => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public PlayerSkillExecuter(
            IBattleObservable battleObservable,
            IScoreController scoreController,
            ITransformController transformController,
            IMemberObservable memberObservable,
            ISelectSkillObservable selectSkillObservable,
            ILiberateController liberateController
        )
        {
            _battleObservable = battleObservable;
            _scoreController = scoreController;
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
            int defeatEnemyCount = 0; // “¯Žž‚ÉŒ‚”j‚µ‚½Enemy‚Ì”
            runningSkill.SkillObservable.OnAttackEnable
                .Subscribe(_ => defeatEnemyCount = 0)
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnAttackHit
                .Subscribe(hitData =>
                {
                    var damagedOwner = hitData.DamagedOwner;
                    if (damagedOwner is IEnemyComponentCollection enemyOwner && enemyOwner.Health.Current.Value <= 0) ++defeatEnemyCount;
                })
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnAttackDisable // LiberateŒŸØ
                .Subscribe(attackRange =>
                {
                    var isExistAliveEnemyInAttackRange =
                        _battleObservable.EnemyList.Any(enemy => attackRange.Contains(enemy.TransformController.GetLandedHex()) && !enemy.DieObservable.IsDead.Value);
                    if (!isExistAliveEnemyInAttackRange) _liberateController.Liberate(attackRange);
                    if (defeatEnemyCount > 1) _scoreController.AcquireScore(ScoreType.DEFEAT_MULTI_ENEMY, defeatEnemyCount);
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
