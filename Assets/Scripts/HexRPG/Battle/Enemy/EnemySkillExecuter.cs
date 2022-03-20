using HexRPG.Battle.Skill;
using HexRPG.Battle.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public class EnemySkillExecuter : ISkillSpawnObservable, ISkillController, ISkillObservable, IDisposable
    {
        ISkillComponentCollection[] ISkillSpawnObservable.SkillList => throw new System.NotImplementedException();

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = true;

        IObservable<Unit> ISkillObservable.OnStartSkill => throw new NotImplementedException();

        IObservable<Unit> ISkillObservable.OnFinishSkill => throw new NotImplementedException();

        ISkillComponentCollection ISkillController.StartSkill(int index, List<Hex> skillRange)
        {
            throw new System.NotImplementedException();
        }

        void IDisposable.Dispose()
        {

        }
    }
}
