using UnityEngine;
using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [TrackColor(1, 0, 0)]
    [TrackClipType(typeof(AttackEnableAsset))]
    public class AttackEnableTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "AttackEnable";
            base.OnCreateClip(clip);
        }
    }
}
