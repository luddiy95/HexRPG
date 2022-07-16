using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Skill;

    public class SkillReservationAsset : PlayableAsset
    {
        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var behaviour = new SkillReservationBehaviour();
            var skillReservation = owner.GetComponent<ISkillReservation>();
            behaviour.Init(skillReservation);
            return ScriptPlayable<SkillReservationBehaviour>.Create(graph, behaviour);
        }
    }
}
