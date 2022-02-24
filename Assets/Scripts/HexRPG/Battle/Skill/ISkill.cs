using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface ISkill
    {
        void Init(PlayableAsset timeline, ICharacterComponentCollection skillOrigin, Animator animator);

        void StartSkill(List<Hex> skillRange);
    }
}
