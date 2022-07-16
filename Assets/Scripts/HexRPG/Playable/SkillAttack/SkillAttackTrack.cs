using UnityEngine;
using UnityEngine.Timeline;

namespace HexRPG.Playable
{
    using Battle.Skill;

    [TrackColor(1, 0, 0)]
    [TrackClipType(typeof(SkillAttackAsset))]
    public class SkillAttackTrack : TrackAsset
    {
        public SkillCenterType skillCenterType = SkillCenterType.SELF;
        public Vector2 skillCenter;

        protected override void OnCreateClip(TimelineClip clip)
        {
            clip.displayName = "SkillAttack";
            base.OnCreateClip(clip);
        }
    }
}
