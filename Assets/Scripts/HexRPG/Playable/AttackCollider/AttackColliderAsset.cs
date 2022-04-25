using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    public class AttackColliderAsset : PlayableAsset
    {
        public AttackColliderBehaviour behaviour = new AttackColliderBehaviour();

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<AttackColliderBehaviour>.Create(graph, behaviour);
        }
    }
}
