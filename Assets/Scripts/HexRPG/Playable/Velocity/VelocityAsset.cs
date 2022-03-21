using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [System.Serializable]
    public class VelocityAsset : PlayableAsset
    {
        public VelocityBehaviour behaviour = new VelocityBehaviour();

        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending; }
        }

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<VelocityBehaviour>.Create(graph, behaviour);
        }
    }
}
