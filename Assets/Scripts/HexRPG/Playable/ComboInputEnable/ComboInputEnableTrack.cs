using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    [TrackColor(0, 1, 0)]
    [TrackClipType(typeof(ComboInputEnableAsset))]
    public class ComboInputEnableTrack : TrackAsset
    {
        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "ComboInputEnable";
            base.OnCreateClip(clip);
        }
    }
}
