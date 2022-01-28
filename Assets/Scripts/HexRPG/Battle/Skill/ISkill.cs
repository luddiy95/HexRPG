using System.Collections.Generic;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public interface ISkill : IFeature
    {
        void Init();

        void StartSkill();

        void FinishSkill();

        void StartEffect();

        void OnFinishEffect();
    }
}
