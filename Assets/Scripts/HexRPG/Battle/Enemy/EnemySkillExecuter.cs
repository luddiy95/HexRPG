using HexRPG.Battle.Stage;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Zenject;
using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    using Skill;

    public class EnemySkillExecuter : ISkillSpawnObservable, ISkillController, ISkillObservable, IInitializable, IDisposable
    {
        IStageController _stageController;
        IBattleObservable _battleObservable;
        IEnemyComponentCollection _enemyOwner;
        ITransformController _transformController;
        List<SkillOwner.Factory> _skillFactories;
        ISkillsSetting _skillsSetting;
        IAttackReserve _attackReserve;
        IAttackController _attackController;

        ISkillComponentCollection[] ISkillSpawnObservable.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        IObservable<Unit> ISkillObservable.OnStartReservation => null;
        IObservable<Unit> ISkillObservable.OnFinishReservation => null;
        IObservable<SkillAttackSetting> ISkillObservable.OnSkillAttackEnable => null;
        IObservable<Unit> ISkillObservable.OnSkillAttackDisable => null;
        IObservable<Hex[]> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemySkillExecuter(
            IStageController stageController,
            IBattleObservable battleObservable,
            IEnemyComponentCollection enemyOwner,
            ITransformController transformController,
            List<SkillOwner.Factory> skillFactories,
            ISkillsSetting skillsSetting,
            IAttackReserve attackReservation,
            IAttackController attackController
        )
        {
            _stageController = stageController;
            _battleObservable = battleObservable;
            _enemyOwner = enemyOwner;
            _transformController = transformController;
            _skillFactories = skillFactories;
            _skillsSetting = skillsSetting;
            _attackReserve = attackReservation;
            _attackController = attackController;
        }

        void IInitializable.Initialize()
        {
            _skillList = _skillFactories.Select((factory, index) => {
                ISkillComponentCollection skillOwner = factory.Create(_transformController.SpawnRootTransform("Skill"), Vector3.zero);
                var skill = _skillsSetting.Skills[index];
                skillOwner.Skill.Init(skill.Timeline, skill.ActivationBindingMap, _enemyOwner.AnimationController);
                return skillOwner;
            }).ToArray();
            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex landedHex, int skillRotation)
        {
            var runningSkill = _skillList[index];

            var skill = runningSkill.Skill;
            var skillCenter = _transformController.GetLandedHex();
            switch (skill.SkillCenterType)
            {
                case SkillCenterType.SELF:
                    // Ž©•ªŽ©g‚Ìê‡landedHex‚Ì‚Ü‚Ü‚Å—Ç‚¢
                    break;
                case SkillCenterType.PLAYER:
                    skillCenter = _battleObservable.PlayerLandedHex;
                    skillRotation = _transformController.GetLookRotationAngleY(_battleObservable.PlayerLandedHex.transform.position);
                    break;
                default:
                    break;
            }

            var curAttackIndicateHexList =
                _stageController.GetHexList(
                    skillCenter,
                    skill.FullAttackRange,
                    _transformController.DefaultRotation + _transformController.RotationAngle + skillRotation);

            _disposables.Clear();
            runningSkill.SkillObservable.OnStartReservation
                .Subscribe(_ => _attackReserve.StartAttackReservation(curAttackIndicateHexList))
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnFinishReservation
                .Subscribe(_ => _attackReserve.FinishAttackReservation())
                .AddTo(_disposables);
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
            runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _onFinishSkill.OnNext(Unit.Default);
                }).AddTo(_disposables);

            skill.StartSkill(skillCenter, _transformController.DefaultRotation + _transformController.RotationAngle + skillRotation);

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
