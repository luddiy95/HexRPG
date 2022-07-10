using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle
{
    using Skill;

    public interface ISkillSpawnController
    {
        void Spawn(IAttackComponentCollection attackOwner, Transform root);
    }

    public interface ISkillSpawnObservable
    {
        List<ISkillComponentCollection> SkillList { get; }
        bool IsAllSkillSpawned { get; }
    }
}
