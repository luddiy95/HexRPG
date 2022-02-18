using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public class AbstractAttackSkillBehaviour : MonoBehaviour, ISkill, IAttackSkill
    {
        [Inject] ISkillComponentCollection _skillOwner;
        [Inject] IAttackController _attackController;

        [SerializeField] protected GameObject _skillEffect;

        List<Hex> _curAttackRange;

        [Inject]
        public void Construct(
            ISkillComponentCollection skillOwner,
            IAttackController attackController)
        {
            _skillOwner = skillOwner;
            _attackController = attackController;
        }

        void Awake()
        {
            _skillEffect.SetActive(false);
        }

        void ISkill.Init()
        {
            _skillEffect.SetActive(false);
        }

        void ISkill.StartSkill()
        {
            _skillEffect.SetActive(false);
        }

        void ISkill.FinishSkill()
        {
            _skillEffect.SetActive(false);
        }

        void ISkill.StartEffect()
        {
            _skillEffect.SetActive(true);
        }

        void IAttackSkill.StartAttackEnable(List<Hex> attackRange, ICharacterComponentCollection attackOrigin)
        {
            var attackSetting = new AttackSetting
            {
                _power = _skillOwner.SkillSetting.Damage
            };
            _curAttackRange = attackRange;

            _attackController.StartAttack(_curAttackRange, attackSetting, attackOrigin);
        }

        void IAttackSkill.FinishAttackEnable()
        {
            _attackController.FinishAttack();
        }

        void ISkill.OnFinishEffect()
        {
            _skillEffect.SetActive(false);
        }
    }
}
