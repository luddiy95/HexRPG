using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Zenject;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Player.Member
{
    using Stage;
    using Battle.Skill;

    public class MemberSkillExecuter : ISkillSpawnObservable, ISkillController, IInitializable, IDisposable
    {
        IMemberComponentCollection _memberOwner;
        ITransformController _transformController;
        IMental _mental;
        List<SkillOwner.Factory> _skillFactories;
        ISkillsSetting _skillsSetting;

        ISkillComponentCollection[] ISkillSpawnObservable.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        ISkillComponentCollection _runningSkill = null;

        CompositeDisposable _disposables = new CompositeDisposable();

        public MemberSkillExecuter(
            IMemberComponentCollection memberOwner,
            ITransformController transformController,
            IMental mental,
            List<SkillOwner.Factory> skillFactories,
            ISkillsSetting skillsSetting
        )
        {
            _memberOwner = memberOwner;
            _transformController = transformController;
            _mental = mental;
            _skillFactories = skillFactories;
            _skillsSetting = skillsSetting;
        }

        void IInitializable.Initialize()
        {
            _skillList = _skillFactories.Select((factory, index) => {
                ISkillComponentCollection skillOwner = factory.Create(_transformController.SpawnRootTransform("Skill"), Vector3.zero);
                skillOwner.Skill.Init(_skillsSetting.Skills[index].Timeline, _memberOwner, _memberOwner.AnimationController);
                return skillOwner;
            }).ToArray();
            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex landedHex, int skillRotation)
        {
            _runningSkill = _skillList[index];
            _mental.Update(-_runningSkill.SkillSetting.MPcost);

            _disposables.Clear();
            _runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _runningSkill = null;
                    _disposables.Clear();
                }).AddTo(_disposables);

            _runningSkill.Skill.StartSkill(landedHex, skillRotation);

            return _runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
