using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;
using System;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Skill;

    [Serializable]
    public class SkillAttackBehaviour : PlayableBehaviour
    {
        ISkillAttack _skillAttack;

        int _damage;
        public string _attackEffectTrack;
        List<Vector2> _attackRange;
        Vector2 _attackEffectOffset; // SkillCenter‚É‘Î‚·‚éOffset

        public void Init(ISkillAttack skillAttack, int damage, string attackEffectTrack, List<Vector2> attackRange, Vector2 attackEffectOffset)
        {
            _skillAttack = skillAttack;

            _damage = damage;
            _attackEffectTrack = attackEffectTrack;
            _attackRange = attackRange;
            _attackEffectOffset = attackEffectOffset;
        }

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            _skillAttack.OnAttackEnable(_damage, _attackEffectTrack, _attackRange, _attackEffectOffset);
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _skillAttack.OnAttackDisable(_attackEffectTrack);
        }
    }
}
