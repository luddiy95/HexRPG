using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface IAttackSkill
    {
        void StartAttackEnable(List<Hex> attackRange, ICharacterComponentCollection attackOrigin);
        void FinishAttackEnable();
    }
}
