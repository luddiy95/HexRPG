using HexRPG.Battle.Skill;
using HexRPG.Battle.Stage;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    public class EnemySkillBehaviour : MonoBehaviour, ISkillSpawnObservable, ISkillController, IAttackSkillController
    {
        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        public ISkillComponentCollection[] SkillList => throw new System.NotImplementedException();
        public ISkillComponentCollection RunningSkill => throw new System.NotImplementedException();

        async UniTaskVoid Start()
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy()); // TransformBehaviourÇ™èâä˙âªÇ≥ÇÍÇÈÇÃÇë“Ç¬
            //TODO: SkillÇFactoryÇ©ÇÁCreate
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
    }
}
