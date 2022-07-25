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
        ISkillsEquipment _skillsEquipment;
        IAttackReserve _attackReserve;
        ILiberateController _liberateController;

        List<ISkillComponentCollection> ISkillSpawnObservable.SkillList => _skillList;
        readonly List<ISkillComponentCollection> _skillList = new List<ISkillComponentCollection>(8);

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        IObservable<Unit> ISkillObservable.OnStartReservation => null;
        IObservable<Unit> ISkillObservable.OnFinishReservation => null;
        IObservable<IEnumerable<Hex>> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        List<Hex> _curAttackIndicateHexList = new List<Hex>(16);

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemySkillExecuter(
            IStageController stageController,
            IBattleObservable battleObservable,
            IEnemyComponentCollection enemyOwner,
            ITransformController transformController,
            List<SkillOwner.Factory> skillFactories,
            ISkillsEquipment skillsEquipment,
            IAttackReserve attackReservation,
            ILiberateController liberateController
        )
        {
            _stageController = stageController;
            _battleObservable = battleObservable;
            _enemyOwner = enemyOwner;
            _transformController = transformController;
            _skillFactories = skillFactories;
            _skillsEquipment = skillsEquipment;
            _attackReserve = attackReservation;
            _liberateController = liberateController;
        }

        void IInitializable.Initialize()
        {
            for (int i = 0; i < _skillFactories.Count; i++)
            {
                var factory = _skillFactories[i];
                ISkillComponentCollection skillOwner = factory.Create(_transformController.SpawnRootTransform("Skill"), Vector3.zero);
                var skill = _skillsEquipment.Skills[i];
                skillOwner.Skill.Init(_enemyOwner, _enemyOwner.AnimationController, skill.Timeline, skill.ActivationBindingObjMap);
                _skillList.Add(skillOwner);
            }

            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex landedHex, int skillRotation)
        {
            var runningSkill = _skillList[index];

            var skill = runningSkill.Skill;
            Hex skillCenter = default;
            switch (skill.SkillCenterType)
            {
                case SkillCenterType.SELF:
                    skillCenter = _stageController.GetHex(
                        _transformController.GetLandedHex(),
                        skill.SkillCenter,
                        MathUtility.GetIntegerEuler60(_transformController.DefaultRotation + _transformController.RotationAngle + skillRotation));
                    break;
                case SkillCenterType.PLAYER:
                    skillCenter = _battleObservable.PlayerLandedHex;
                    skillRotation = _transformController.GetLookRotationAngleY(_battleObservable.PlayerLandedHex.transform.position)
                         - _transformController.DefaultRotation - _transformController.RotationAngle;
                    break;
                default:
                    break;
            }

            _stageController.GetHexList(
                skillCenter,
                skill.FullAttackRange,
                MathUtility.GetIntegerEuler60(_transformController.DefaultRotation + _transformController.RotationAngle + skillRotation),
                ref _curAttackIndicateHexList);

            _disposables.Clear();
            var skillObservable = runningSkill.SkillObservable;
            skillObservable.OnStartReservation
                .Subscribe(_ => _attackReserve.StartAttackReservation(_curAttackIndicateHexList))
                .AddTo(_disposables);
            skillObservable.OnFinishReservation
                .Subscribe(_ => _attackReserve.FinishAttackReservation())
                .AddTo(_disposables);

            skillObservable.OnSkillAttack
                .Subscribe(attackRange =>
                {
                    var isExistPlayerInAttackRange = attackRange.Contains(_battleObservable.PlayerLandedHex); //TODO: Player‚ª¶‚«‚Ä‚¢‚é‚©
                    if (!isExistPlayerInAttackRange) _liberateController.Liberate(attackRange);
                })
                .AddTo(_disposables);
            skillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _onFinishSkill.OnNext(Unit.Default);
                    _disposables.Clear();
                }).AddTo(_disposables);

            skill.StartSkill(skillCenter,
                MathUtility.GetIntegerEuler60(_transformController.DefaultRotation + _transformController.RotationAngle + skillRotation));

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
