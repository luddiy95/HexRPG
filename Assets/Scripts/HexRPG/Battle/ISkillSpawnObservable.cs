using UnityEngine;

namespace HexRPG.Battle
{
    using Skill;

    public interface ISkillSpawnController
    {
        void Spawn(Transform root);
    }

    public interface ISkillSpawnObservable
    {
        ISkillComponentCollection[] SkillList { get; }
        bool IsAllSkillSpawned { get; }
    }
}
