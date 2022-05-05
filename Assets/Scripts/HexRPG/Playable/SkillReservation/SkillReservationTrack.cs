using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [TrackColor(1, 1, 0)]
    [TrackClipType(typeof(SkillReservationAsset))]
    public class SkillReservationTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "SkillReservation";
            base.OnCreateClip(clip);
        }
    }
}
