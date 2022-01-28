using System.Collections.Generic;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface IAttackSkill : IFeature
    {
        void StartAttackEnable(List<Hex> attackRange, ICustomComponentCollection attackOrigin);
        void FinishAttackEnable();
    }
}
