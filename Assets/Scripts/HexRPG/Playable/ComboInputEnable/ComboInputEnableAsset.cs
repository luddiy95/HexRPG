using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    using HexRPG.Battle.Combat;

    [System.Serializable]
    public class ComboInputEnableAsset : PlayableAsset
    {
        public override UnityEngine.Playables.Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var comboInputEnable = owner.GetComponent<IComboInputEnable>();
            var behaviour = new ComboInputEnableBehaviour();
            behaviour.Init(comboInputEnable);
            return ScriptPlayable<ComboInputEnableBehaviour>.Create(graph, behaviour);
        }
    }
}
