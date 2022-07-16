using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public interface ISkillAttack
    {
        void OnAttackEnable(int damage, string attackEffectTrack, List<Vector2> attackRange, Vector2 attackEffectOffset);
        void OnAttackDisable(string attackEffectTrack);
    }
}
