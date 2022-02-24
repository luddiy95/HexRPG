using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface IAttackSkill
    {
        void StartAttackEnable();
        void FinishAttackEnable();
    }
}
