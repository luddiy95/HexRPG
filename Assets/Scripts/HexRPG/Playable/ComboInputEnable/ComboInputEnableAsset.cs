using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    [System.Serializable]
    public class ComboInputEnableAsset : PlayableAsset
    {
        public ComboInputEnableBehaviour behaviour = new ComboInputEnableBehaviour();

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<ComboInputEnableBehaviour>.Create(graph, behaviour);
        }
    }
}
