using UnityEngine;
using Cinemachine;

namespace HexRPG.Battle.Skill
{
    public interface ISkill
    {
        void Init();

        void StartSkill();

        void FinishSkill();

        void StartEffect();

        void OnFinishEffect();
    }
}
