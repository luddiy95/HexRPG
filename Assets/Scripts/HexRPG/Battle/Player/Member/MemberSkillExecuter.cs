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

    public class MemberSkillExecuter : MonoBehaviour, ISkillSpawnObservable, ISkillController
    {
        IMemberComponentCollection _memberOwner;
        ITransformController _transformController;
        IAnimatorController _animatorController;
        IMental _mental;
        List<SkillOwner.Factory> _skillFactories;
        ISkills _skills;

        bool ISkillSpawnObservable.IsAllSkillSpawned => _isAllSkillSpawned;
        bool _isAllSkillSpawned = false;

        ISkillComponentCollection[] ISkillController.SkillList => _skillList;
        ISkillComponentCollection[] _skillList;

        ISkillComponentCollection _runningSkill = null;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IMemberComponentCollection memberOwner,
            ITransformController transformController,
            IAnimatorController animatorController,
            IMental mental,
            List<SkillOwner.Factory> skillFactories,
            ISkills skills
        )
        {
            _memberOwner = memberOwner;
            _transformController = transformController;
            _animatorController = animatorController;
            _mental = mental;
            _skillFactories = skillFactories;
            _skills = skills;
        }

        async UniTaskVoid Start()
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy()); // TransformBehaviour‚ª‰Šú‰»‚³‚ê‚é‚Ì‚ð‘Ò‚Â
            _skillList = _skillFactories.Select((factory, index) => {
                ISkillComponentCollection skillOwner = factory.Create(_transformController.SpawnRootTransform, Vector3.zero);
                skillOwner.Skill.Init(_skills.Skills[index].Timeline, _memberOwner, _animatorController.Animator);
                return skillOwner;
            }).ToArray();
            _isAllSkillSpawned = true;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, List<Hex> skillRange)
        {
            _runningSkill = _skillList[index];
            _mental.Update(-_runningSkill.SkillSetting.MPcost);

            _disposables.Clear();
            _runningSkill.SkillObservable.OnFinishSkill.Subscribe(_ =>
            {
                _runningSkill = null;
                _disposables.Clear();
            }).AddTo(_disposables);

            _runningSkill.Skill.StartSkill(skillRange);

            return _runningSkill;
        }
    }
}
