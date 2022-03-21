using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [TrackColor(1, 0, 0)]
    [TrackClipType(typeof(VelocityAsset))]
    public class VelocityTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "Velocity";
            base.OnCreateClip(clip);
        }
    }
}
