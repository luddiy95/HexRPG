using UnityEngine.Playables;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    [System.Serializable]
    public class AttackEnablePlayableAsset : PlayableAsset
    {
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            return ScriptPlayable<AttackEnableBehaviour>.Create(graph);
        }
    }
}
