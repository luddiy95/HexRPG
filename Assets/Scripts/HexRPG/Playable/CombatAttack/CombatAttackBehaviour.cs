using UnityEngine;
using UnityEngine.Playables;
using System;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Combat;

    [Serializable]
    public class CombatAttackBehaviour : PlayableBehaviour
    {
        ICombatAttack _combatAttack;

        int _damage;
        Vector3 _direction;
        float _speed;

        Vector3 Velocity => _direction.normalized * _speed;

        public void Init(ICombatAttack combatAttack, int damage, Vector3 direction, float speed)
        {
            _combatAttack = combatAttack;
            _damage = damage;
            _direction = direction;
            _speed = speed;
        }

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);

            _combatAttack.OnAttackEnable(_damage, Velocity);
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _combatAttack.OnAttackDisable();
        }
    }
}
