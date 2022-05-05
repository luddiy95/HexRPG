using UnityEngine;
using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    using Battle.Skill;

    [TrackColor(1, 0, 0)]
    [TrackClipType(typeof(AttackEnableAsset))]
    public class AttackEnableTrack : TrackAsset
    {
        public SkillCenterType skillCenterType = SkillCenterType.SELF;
        public Vector2 skillCenter;

        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "AttackEnable";
            base.OnCreateClip(clip);
        }
    }
}
