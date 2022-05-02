using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    public enum SkillCenterType
    {
        SELF,
        PLAYER,
        NEAREST_ENEMY
    }

    [TrackColor(1, 0, 0)]
    [TrackClipType(typeof(AttackEnableAsset))]
    public class AttackEnableTrack : TrackAsset
    {
        public SkillCenterType skillCenterType = SkillCenterType.SELF;

        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "AttackEnable";
            base.OnCreateClip(clip);
        }
    }
}
