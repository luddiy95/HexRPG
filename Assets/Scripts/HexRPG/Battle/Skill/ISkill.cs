using UnityEngine;
using Cinemachine;

namespace HexRPG.Battle.Skill
{
    public interface ISkill : IFeature
    {
        void Init();

        void StartSkill();

        void FinishSkill();

        void StartEffect();

        void OnFinishEffect();
    }
}
