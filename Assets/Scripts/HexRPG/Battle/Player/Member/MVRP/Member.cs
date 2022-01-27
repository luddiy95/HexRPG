using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Skill;

    public interface ICharacterSkillCallback
    {
        void StartSkillAnimation(string animationParam);
    }

    public class Member : MonoBehaviour
    {
        Sprite _statusIcon;
        public Sprite StatusIcon => _statusIcon;

        Sprite _icon;
        public Sprite Icon => _icon;

        int _maxHP;
        public int MaxHP => _maxHP;
        int _maxMP;
        public int MaxMP => _maxMP;

        int _hp;
        public int HP => _hp;
        int _mp;
        public int MP => _mp;

        ICharacterSkillCallback _skillCallback;

        List<ISkill> _skillList = new List<ISkill>();
        public List<ISkill> SkillList => _skillList;

        public ISkill RunningSkill => _runningSkill;
#nullable enable
        ISkill? _runningSkill = null;
#nullable disable

        public void Init(MemberData memberData, ICharacterSkillCallback skillCallback)
        {
            /*
            _statusIcon = memberData.StatusIcon;
            _icon = memberData.Icon;

            _maxHP = _hp = memberData.MaxHP;
            _maxMP = _mp = memberData.MaxMP;

            GameObject skillRoot = new GameObject("SkillRoot");
            skillRoot.transform.parent = transform;
            memberData.SkillPrefabList.ForEach(prefab =>
            {
                _skillList.Add(Instantiate(prefab, skillRoot.transform));
            });

            _skillCallback = skillCallback;
            */
        }

        public bool TryExecuteSkill(int index)
        {
            ISkill skill = _skillList[index];
            //if (_mp < skill.MPcost) return false;
            StartSkill(skill);
            return true;
        }

        void StartSkill(ISkill skill)
        {
            //_mp -= skill.MPcost;
            _runningSkill = skill;
            skill.StartSkill();
            //_skillCallback.StartSkillAnimation(skill.SkillAnimationParam);
        }

        public void OnFinishSkill()
        {
            _runningSkill.FinishSkill();
            _runningSkill = null;
        }

#nullable enable
        public void StartSkillEffect() => _runningSkill?.StartEffect();
#nullable disable
    }
}
