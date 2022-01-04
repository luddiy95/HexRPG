using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Player.Character
{
    using Skill;

    public interface ICharacterSkillCallback
    {
        void StartSkillAnimation(string animationParam);
    }

    public class Character : MonoBehaviour
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

        List<BaseSkill> _skillList = new List<BaseSkill>();
        public List<BaseSkill> SkillList => _skillList;

#nullable enable
        BaseSkill? _runningSkill = null;
#nullable disable

        public void Init(CharacterData characterData, ICharacterSkillCallback skillCallback)
        {
            _statusIcon = characterData.StatusIcon;
            _icon = characterData.Icon;

            _maxHP = _hp = characterData.MaxHP;
            _maxMP = _mp = characterData.MaxMP;

            GameObject skillRoot = new GameObject("SkillRoot");
            skillRoot.transform.parent = transform;
            characterData.SkillPrefabList.ForEach(prefab =>
            {
                _skillList.Add(Instantiate(prefab, skillRoot.transform));
            });

            _skillCallback = skillCallback;
        }

        public bool TryExecuteSkill(int index)
        {
            BaseSkill skill = _skillList[index];
            if (_mp < skill.MPcost) return false;
            StartSkill(skill);
            return true;
        }

        void StartSkill(BaseSkill skill)
        {
            _mp -= skill.MPcost;
            _runningSkill = skill;
            skill.StartSkill();
            _skillCallback.StartSkillAnimation(skill.SkillAnimationParam);
        }

#nullable enable
        public void StartSkillEffect() => _runningSkill?.StartEffect();
#nullable disable
    }
}
