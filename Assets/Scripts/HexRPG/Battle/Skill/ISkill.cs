using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface ISkill
    {
        void Init(IAttackComponentCollection attackOwner, IAnimationController animationController, PlayableAsset timeline, ActivationBindingObjDictionary activationBindingObjMap);
        void HideEffect();
        void StartSkill(Hex skillCenter, int skillRotation);

        PlayableAsset PlayableAsset { get; }
        List<Vector2> FullAttackRange { get; }
        SkillCenterType SkillCenterType { get; }
        Vector2 SkillCenter { get; }
    }
}
