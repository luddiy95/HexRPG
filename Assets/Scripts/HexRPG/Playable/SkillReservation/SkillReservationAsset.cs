using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    public class SkillReservationAsset : PlayableAsset
    {
        public SkillReservationBehaviour behaviour = new SkillReservationBehaviour();

        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<SkillReservationBehaviour>.Create(graph, behaviour);
        }
    }
}
