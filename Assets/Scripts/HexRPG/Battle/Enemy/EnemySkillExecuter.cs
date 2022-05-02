using HexRPG.Battle.Skill;
using HexRPG.Battle.Stage;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Zenject;
using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public class EnemySkillExecuter : ISkillSpawnObservable, ISkillController, ISkillObservable, IInitializable, IDisposable
    {
        IEnemyComponentCollection _enemyOwner;
        ITransformController _transformController;
        List<SkillOwner.Factory> _skillFactories;
        ISkillsSetting _skillsSetting;

        ISkillComponentCollection[] ISkillSpawnObservable.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        IObservable<Hex[]> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemySkillExecuter(
            IEnemyComponentCollection enemyOwner,
            ITransformController transformController,
            List<SkillOwner.Factory> skillFactories,
            ISkillsSetting skillsSetting
        )
        {
            _enemyOwner = enemyOwner;
            _transformController = transformController;
            _skillFactories = skillFactories;
            _skillsSetting = skillsSetting;
        }

        void IInitializable.Initialize()
        {
            _skillList = _skillFactories.Select((factory, index) => {
                ISkillComponentCollection skillOwner = factory.Create(_transformController.SpawnRootTransform("Skill"), Vector3.zero);
                skillOwner.Skill.Init(_skillsSetting.Skills[index].Timeline, _enemyOwner, _enemyOwner.AnimationController);
                return skillOwner;
            }).ToArray();
            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex landedHex, int skillRotation)
        {
            var runningSkill = _skillList[index];

            _disposables.Clear();
            runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _onFinishSkill.OnNext(Unit.Default);
                }).AddTo(_disposables);

            var skillCenter = _transformController.GetLandedHex();
            skillRotation = 0; //TODO: skillRotationÇ«Ç§Ç∑ÇÈÅH
            switch (runningSkill.Skill.SkillCenterType)
            {
                case Playable.SkillCenterType.SELF:
                    // é©ï™é©êgÇÃèÍçálandedHexÇÃÇ‹Ç‹Ç≈ó«Ç¢
                    break;
                case Playable.SkillCenterType.PLAYER:
                    //TODO: PlayerÇÃà íuÇ…Ç∑ÇÈ
                    break;
                default:
                    break;
            }

            runningSkill.Skill.StartSkill(skillCenter, 0);

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
