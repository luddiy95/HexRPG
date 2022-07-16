using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Combat;

    public class CombatAttackAsset : PlayableAsset
    {
        public int damage;
        public Vector3 direction;
        public float speed;

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var combatAttack = owner.GetComponent<ICombatAttack>();
            var behaviour = new CombatAttackBehaviour();
            behaviour.Init(combatAttack, damage, direction, speed);
            return ScriptPlayable<CombatAttackBehaviour>.Create(graph, behaviour);
        }
    }
}
