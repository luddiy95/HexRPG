using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    using HexRPG.Battle;

    [TrackColor(1, 0, 0)]
    [TrackBindingType(typeof(AttackCollider))]
    [TrackClipType(typeof(CombatAttackAsset))]
    public class CombatAttackTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "CombatAttack";
            base.OnCreateClip(clip);
        }
    }
}
