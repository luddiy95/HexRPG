using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    [System.Serializable]
    public class AttackEnableAsset : PlayableAsset
    {
        public AttackEnableBehaviour behaviour = new AttackEnableBehaviour();

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<AttackEnableBehaviour>.Create(graph, behaviour);
        }
    }
}
