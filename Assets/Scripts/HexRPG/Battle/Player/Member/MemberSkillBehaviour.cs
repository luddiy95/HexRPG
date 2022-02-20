using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Zenject;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Player.Member
{
    using Stage;
    using Skill;

    public class MemberSkillBehaviour : MonoBehaviour, ISkillSpawnObservable, ISkillController, IAttackSkillController
    {
        IMemberComponentCollection _memberOwner;
        ITransformController _transformController;
        IMental _mental;
        List<SkillOwner.Factory> _skillFactories;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        ISkillComponentCollection[] ISkillController.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        ISkillComponentCollection ISkillController.RunningSkill => _runningSkill;
        ISkillComponentCollection _runningSkill = null;

        List<Hex> _curAttackRange;

        [Inject]
        public void Construct(
            IMemberComponentCollection memberOwner,
            ITransformController transformController,
            IMental mental,
            List<SkillOwner.Factory> skillFactories
        )
        {
            _memberOwner = memberOwner;
            _transformController = transformController;
            _mental = mental;
            _skillFactories = skillFactories;
        }

        async UniTaskVoid Start()
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy()); // TransformBehaviour‚ª‰Šú‰»‚³‚ê‚é‚Ì‚ð‘Ò‚Â
            _skillList = _skillFactories.Select(factory => factory.Create(_transformController.SpawnRootTransform, Vector3.zero)).ToArray();
            _isAllSkillSpawned = true;
        }

        bool ISkillController.TryStartSkill(int index, List<Hex> attackRange)
        {
            var skillOwner = _skillList[index];
            var skillSetting = skillOwner.SkillSetting;
            var MPcost = skillSetting.MPcost;
            if (_mental.Current.Value < MPcost) return false;
            StartSkill(skillOwner, MPcost);
            _curAttackRange = attackRange;
            return true;
        }

        void StartSkill(ISkillComponentCollection skillOwner, int MPcost)
        {
            _mental.Update(-MPcost);
            _runningSkill = skillOwner;

            skillOwner.Skill.StartSkill();
        }

        void ISkillController.FinishSkill()
        {
            _runningSkill.Skill.FinishSkill();
            _runningSkill = null;
        }

        void ISkillController.StartSkillEffect()
        {
            _runningSkill.Skill.StartEffect();
        }

        void IAttackSkillController.StartSkillAttackEnable()
        {
            _runningSkill.AttackSkill.StartAttackEnable(_curAttackRange, _memberOwner);
        }

        void IAttackSkillController.FinishSkillAttackEnable()
        {
            _runningSkill.AttackSkill.FinishAttackEnable();
        }
    }
}
