using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface ISkill
    {
        void Init(PlayableAsset timeline, ICharacterComponentCollection skillOrigin, IAnimationController memberAnimationController);
        void StartSkill(Hex landedHex, int skillRotation);

        PlayableAsset PlayableAsset { get; }
        List<Vector2> FullAttackRange { get; }
    }
}
