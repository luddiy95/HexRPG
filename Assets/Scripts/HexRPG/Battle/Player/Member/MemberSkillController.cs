using UnityEngine;
using System.Linq;

namespace HexRPG.Battle.Player.Member
{
    using Skill;

    public class MemberSkillController : AbstractCustomComponentBehaviour, ISkillController
    {
        IMental _mental;

        BaseSkill[] _skillList;
        
        BaseSkill ISkillController.RunningSkill => _runningSkill;
#nullable enable
        BaseSkill? _runningSkill = null;
#nullable disable

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISkillController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _mental);
            if(Owner.QueryInterface(out ISkillListSetting skillListSetting))
            {
                var skillList = skillListSetting.SkillList;

                GameObject skillRoot = new GameObject("SkillRoot");
                skillRoot.transform.parent = transform;

                _skillList = Enumerable.Range(0, skillList.Length).Select(i => Instantiate(skillList[i], skillRoot.transform)).ToArray();
            }

        }

        bool ISkillController.TryStartSkill(int index)
        {
            BaseSkill skill = _skillList[index];
            if (_mental.Current.Value < skill.MPcost) return false;
            StartSkill(skill);
            return true;
        }

        void StartSkill(BaseSkill skill)
        {
            _mental.Update(-skill.MPcost);
            _runningSkill = skill;
            skill.StartSkill();
        }

        void ISkillController.FinishSkill()
        {
            _runningSkill.FinishSkill();
            _runningSkill = null;
        }

        public void StartSkillEffect()
        {
#nullable enable
            _runningSkill?.StartEffect();
#nullable disable
        }
    }
}
