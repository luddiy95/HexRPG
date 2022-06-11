using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Player.Member
{
    using Stage;
    using Battle.Skill;

    public class MemberSkillExecuter : ISkillSpawnController, ISkillSpawnObservable, ISkillController
    {
        IMemberComponentCollection _memberOwner;
        ISkillPoint _skillPoint;
        List<SkillOwner.Factory> _skillFactories;
        ISkillsSetting _skillsSetting;

        ISkillComponentCollection[] ISkillSpawnObservable.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        public MemberSkillExecuter(
            IMemberComponentCollection memberOwner,
            ISkillPoint skillPoint,
            List<SkillOwner.Factory> skillFactories,
            ISkillsSetting skillsSetting
        )
        {
            _memberOwner = memberOwner;
            _skillPoint = skillPoint;
            _skillFactories = skillFactories;
            _skillsSetting = skillsSetting;
        }

        void ISkillSpawnController.Spawn(IAttackComponentCollection attackOwner, Transform root)
        {
            _skillList = _skillFactories.Select((factory, index) => {
                ISkillComponentCollection skillOwner = factory.Create(root, Vector3.zero);
                var skill = _skillsSetting.Skills[index];
                skillOwner.Skill.Init(attackOwner, _memberOwner.AnimationController, skill.Timeline, skill.ActivationBindingObjMap);
                skillOwner.SkillSetting.SetCost(skill.Cost);
                return skillOwner;
            }).ToArray();
            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex skillCenter, int skillRotation)
        {
            var runningSkill = _skillList[index];

            _skillPoint.Update(-runningSkill.SkillSetting.Cost);

            runningSkill.Skill.StartSkill(skillCenter, skillRotation);

            return runningSkill;
        }
    }
}
