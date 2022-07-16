using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Skill;

    [System.Serializable]
    public class SkillAttackAsset : PlayableAsset
    {
        public int damage;
        public string attackEffectTrack;
        public List<Vector2> attackRange;
        public Vector2 attackEffectOffset; // SkillCenter‚É‘Î‚·‚éOffset

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var behaviour = new SkillAttackBehaviour();
            var skillAttack = owner.GetComponent<ISkillAttack>();
            behaviour.Init(skillAttack, damage, attackEffectTrack, attackRange, attackEffectOffset);
            return ScriptPlayable<SkillAttackBehaviour>.Create(graph, behaviour);
        }
    }
}
