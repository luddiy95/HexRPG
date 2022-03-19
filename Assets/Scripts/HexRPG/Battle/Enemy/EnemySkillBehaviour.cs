using HexRPG.Battle.Skill;
using HexRPG.Battle.Stage;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

namespace HexRPG.Battle.Enemy
{
    public class EnemySkillBehaviour : MonoBehaviour, ISkillSpawnObservable, ISkillController
    {
        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        public ISkillComponentCollection[] SkillList => throw new System.NotImplementedException();
        public ISkillComponentCollection RunningSkill => throw new System.NotImplementedException();

        async UniTaskVoid Start()
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy()); // TransformBehaviourが初期化されるのを待つ
            //TODO: SkillをFactoryからCreate
            _isAllSkillSpawned = true;
        }

        public void StartSkillEffect()
        {
            throw new System.NotImplementedException();
        }

        public void FinishSkill()
        {
            throw new System.NotImplementedException();
        }

        public void StartSkillAttackEnable()
        {
            throw new System.NotImplementedException();
        }

        public void FinishSkillAttackEnable()
        {
            throw new System.NotImplementedException();
        }

        public bool TryStartSkill(int index)
        {
            throw new System.NotImplementedException();
        }

        public void StartSkill(List<Hex> attackRange)
        {
            throw new System.NotImplementedException();
        }

        public ISkillComponentCollection StartSkill(int index, List<Hex> skillRange)
        {
            throw new System.NotImplementedException();
        }
    }
}
