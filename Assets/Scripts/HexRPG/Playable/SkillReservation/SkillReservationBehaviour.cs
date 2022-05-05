using UnityEngine.Playables;
using UniRx;
using System;

namespace HexRPG.Playable
{
    public class SkillReservationBehaviour : PlayableBehaviour
    {
        public IObservable<Unit> OnStartReservation => _onStartReservation;
        readonly ISubject<Unit> _onStartReservation = new Subject<Unit>();

        public IObservable<Unit> OnFinishReservation => _onFinishReservation;
        readonly ISubject<Unit> _onFinishReservation = new Subject<Unit>();

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);

            _onStartReservation.OnNext(Unit.Default);
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _onFinishReservation.OnNext(Unit.Default);
        }
    }
}
