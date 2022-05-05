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

    public class MemberSkillExecuter : ISkillSpawnController, ISkillSpawnObservable, ISkillController
    {
        IMemberComponentCollection _memberOwner;
        IMental _mental;
        List<SkillOwner.Factory> _skillFactories;
        ISkillsSetting _skillsSetting;

        ISkillComponentCollection[] ISkillSpawnObservable.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        public MemberSkillExecuter(
            IMemberComponentCollection memberOwner,
            IMental mental,
            List<SkillOwner.Factory> skillFactories,
            ISkillsSetting skillsSetting
        )
        {
            _memberOwner = memberOwner;
            _mental = mental;
            _skillFactories = skillFactories;
            _skillsSetting = skillsSetting;
        }

        void ISkillSpawnController.Spawn(Transform root)
        {
            _skillList = _skillFactories.Select((factory, index) => {
                ISkillComponentCollection skillOwner = factory.Create(root, Vector3.zero);
                var skill = _skillsSetting.Skills[index];
                skillOwner.Skill.Init(skill.Timeline, skill.ActivationBindingMap, _memberOwner, _memberOwner.AnimationController);
                return skillOwner;
            }).ToArray();
            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex skillCenter, int skillRotation)
        {
            var runningSkill = _skillList[index];

            _mental.Update(-runningSkill.SkillSetting.MPcost);

            runningSkill.Skill.StartSkill(skillCenter, skillRotation);

            return runningSkill;
        }
    }
}
