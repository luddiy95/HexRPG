using UnityEngine.Playables;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Skill;

    public class SkillReservationBehaviour : PlayableBehaviour
    {
        ISkillReservation _skillReservation;

        public void Init(ISkillReservation skillReservation)
        {
            _skillReservation = skillReservation;
        }

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);

            _skillReservation.OnStartReservation();
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _skillReservation.OnFinishReservation();
        }
    }
}
