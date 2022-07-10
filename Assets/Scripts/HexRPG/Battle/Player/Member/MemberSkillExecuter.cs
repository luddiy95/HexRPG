using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Stage;
    using Battle.Skill;

    public class MemberSkillExecuter : ISkillSpawnController, ISkillSpawnObservable, ISkillController
    {
        IAnimationController _animationController;
        ISkillPoint _skillPoint;
        List<SkillOwner.Factory> _skillFactories;
        ISkillsEquipment _skillsEquipment;

        List<ISkillComponentCollection> ISkillSpawnObservable.SkillList => _skillList;
        readonly List<ISkillComponentCollection> _skillList = new List<ISkillComponentCollection>(8);

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        public MemberSkillExecuter(
            IAnimationController animationController,
            ISkillPoint skillPoint,
            List<SkillOwner.Factory> skillFactories,
            ISkillsEquipment skillsEquipment
        )
        {
            _animationController = animationController;
            _skillPoint = skillPoint;
            _skillFactories = skillFactories;
            _skillsEquipment = skillsEquipment;
        }

        void ISkillSpawnController.Spawn(IAttackComponentCollection attackOwner, Transform root)
        {
            for (int i = 0; i < _skillFactories.Count; i++)
            {
                var factory = _skillFactories[i];
                ISkillComponentCollection skillOwner = factory.Create(root, Vector3.zero);
                var skill = _skillsEquipment.Skills[i];
                skillOwner.Skill.Init(attackOwner, _animationController, skill.Timeline, skill.ActivationBindingObjMap);
                skillOwner.SkillSetting.SetCost(skill.Cost);
                _skillList.Add(skillOwner);
            }
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
