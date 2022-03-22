using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Playable
{
    public static class PlayableExtensions
    {
        public static bool IsClipEnded(this UnityEngine.Playables.Playable playable, FrameData info)
        {
            var duration = playable.GetDuration();
            var time = playable.GetTime();
            var count = time + info.deltaTime;

            return (info.effectivePlayState == PlayState.Paused && count > duration) || Mathf.Approximately((float)time, (float)duration);
        }
    }
}
