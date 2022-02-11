using System.Collections.Generic;
using UnityEngine;
using System;

namespace HexRPG.Battle.Player.Member
{
    using Skill;

    [Serializable]
    public class MemberData
    {
        [SerializeField] Member _memberPrefab;
        public Member MemberPrefab => _memberPrefab;
        [SerializeField] Sprite _statusIcon;
        public Sprite StatusIcon => _statusIcon;
        [SerializeField] Sprite _icon;
        public Sprite Icon => _icon;
        //[SerializeField] List<BaseSkill> _skillPrefabList = new List<BaseSkill>();
        //public List<BaseSkill> SkillPrefabList => _skillPrefabList; 
        [SerializeField] int _maxHP;
        public ref readonly int MaxHP => ref _maxHP;
        [SerializeField] int _maxMP;
        public ref readonly int MaxMP => ref _maxMP;
    }
}