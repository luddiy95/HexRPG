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

    public class MemberSkillExecuter : ISkillSpawnObservable, ISkillController, IInitializable
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
            var runningSkill = _skillList[index];

            _mental.Update(-runningSkill.SkillSetting.MPcost);

            var skillCenter = landedHex;
            switch (runningSkill.Skill.SkillCenterType)
            {
                case Playable.SkillCenterType.SELF:
                    // é©ï™é©êgÇÃèÍçálandedHexÇÃÇ‹Ç‹Ç≈ó«Ç¢
                    break;
                case Playable.SkillCenterType.NEAREST_ENEMY:
                    // é©ï™Ç©ÇÁç≈Ç‡ãﬂÇ¢ìG
                    break;
                default:
                    break;
            }

            runningSkill.Skill.StartSkill(skillCenter, skillRotation);

            return runningSkill;
        }
    }
}
