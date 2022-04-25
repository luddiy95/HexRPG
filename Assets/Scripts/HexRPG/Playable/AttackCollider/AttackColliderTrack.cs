using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    using HexRPG.Battle;

    [TrackColor(1, 0, 0)]
    [TrackBindingType(typeof(AttackCollider))]
    [TrackClipType(typeof(AttackColliderAsset))]
    public class AttackColliderTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "AttackCollider";
            base.OnCreateClip(clip);
        }
    }
}
