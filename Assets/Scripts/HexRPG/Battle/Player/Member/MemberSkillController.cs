using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace HexRPG.Battle.Player.Member
{
    using Stage;
    using Skill;

    public class MemberSkillController : AbstractCustomComponentBehaviour, ISkillController, IAttackSkillController
    {
        IComponentCollectionFactory _factory;
        IMental _mental;

        ICustomComponentCollection[] ISkillController.SkillList => _skillList;
        ICustomComponentCollection[] _skillList;

        ICustomComponentCollection ISkillController.RunningSkill => _runningSkill;
        ICustomComponentCollection _runningSkill = null;

        List<Hex> _curAttackRange;

        GameObject _skillRoot;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISkillController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _factory);
            Owner.QueryInterface(out _mental);

            if(Owner.QueryInterface(out ISkillListSetting skillListSetting))
            {
                var skillList = skillListSetting.SkillList;

                _skillRoot = new GameObject("SkillRoot");
                if(Owner.QueryInterface(out ITransformController transformController))
                {
                    _skillRoot.transform.SetParent(transformController.SpawnRootTransform, true);
                }
                _skillList = Enumerable.Range(0, skillList.Length).Select(i => SpawnSkill(skillList[i])).ToArray();
            }
        }

        ICustomComponentCollection SpawnSkill(GameObject prefab)
        {
            var obj = _factory.CreateComponentCollection(prefab, null, null);

            if (obj.QueryInterface(out ITransformController transformController))
            {
                transformController.RootTransform.SetParent(_skillRoot.transform, true);
            }

            return obj;
        }

        bool ISkillController.TryStartSkill(int index, List<Hex> attackRange)
        {
            var obj = _skillList[index];
            if (!obj.QueryInterface(out ISkillSetting skillSetting)) return false;
            var MPcost = skillSetting.MPcost;
            if (_mental.Current.Value < MPcost) return false;
            StartSkill(obj, MPcost);
            _curAttackRange = attackRange;
            return true;
        }

        void StartSkill(ICustomComponentCollection obj, int MPcost)
        {
            _mental.Update(-MPcost);
            _runningSkill = obj;

            _runningSkill.QueryInterface(out ISkill skill);
            skill.StartSkill();
        }

        void ISkillController.FinishSkill()
        {
            _runningSkill.QueryInterface(out ISkill skill);
            skill.FinishSkill();
            _runningSkill = null;
        }

        void ISkillController.StartSkillEffect()
        {
            _runningSkill.QueryInterface(out ISkill skill);
            skill.StartEffect();
        }

        void IAttackSkillController.StartSkillAttackEnable()
        {
            if (!_runningSkill.QueryInterface(out IAttackSkill attackSkill)) return;
            attackSkill.StartAttackEnable(_curAttackRange, Owner);
        }

        void IAttackSkillController.FinishSkillAttackEnable()
        {
            if (!_runningSkill.QueryInterface(out IAttackSkill attackSkill)) return;
            attackSkill.FinishAttackEnable();
        }
    }
}
