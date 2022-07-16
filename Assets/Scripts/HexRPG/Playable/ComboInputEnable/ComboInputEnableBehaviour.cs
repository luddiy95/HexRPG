using UnityEngine.Playables;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Combat;

    public class ComboInputEnableBehaviour : PlayableBehaviour
    {
        IComboInputEnable _comboInputEnable;

        public void Init(IComboInputEnable comboInputEnable)
        {
            _comboInputEnable = comboInputEnable;
        }

        public override void OnBehaviourPlay(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            _comboInputEnable.ComboInputEnable();
        }

        public override void OnBehaviourPause(UnityEngine.Playables.Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);

            if (!playable.IsClipEnded(info)) return;

            _comboInputEnable.ComboInputDisable();
        }
    }
}
