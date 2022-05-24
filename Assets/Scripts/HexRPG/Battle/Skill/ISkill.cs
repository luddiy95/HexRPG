using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Playable;
    using Stage;

    public interface ISkill
    {
        void Init(PlayableAsset timeline, List<ActivationBindingData> activationBindingMap, IAnimationController animationController);
        void StartSkill(Hex skillCenter, int skillRotation);

        PlayableAsset PlayableAsset { get; }
        List<Vector2> FullAttackRange { get; }
        SkillCenterType SkillCenterType { get; }
        Vector2 SkillCenter { get; }
    }
}
