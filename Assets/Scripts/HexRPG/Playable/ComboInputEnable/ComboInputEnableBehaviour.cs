using System;
using UniRx;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    public class ComboInputEnableBehaviour : PlayableBehaviour
    {
        public IObservable<Unit> OnComboInputEnable => _onComboInputEnable;
        readonly ISubject<Unit> _onComboInputEnable = new Subject<Unit>();
        public IObservable<Unit> OnComboInputDisable => _onComboInputDisable;
        readonly ISubject<Unit> _onComboInputDisable = new Subject<Unit>();

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            _onComboInputEnable.OnNext(Unit.Default);
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _onComboInputDisable.OnNext(Unit.Default);
        }
    }
}
