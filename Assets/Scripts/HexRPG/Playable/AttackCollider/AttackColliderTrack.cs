using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [TrackColor(1, 0, 0)]
    [TrackBindingType(typeof(Collider))]
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
