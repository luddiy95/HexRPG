using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    using Stage;

    public class AbstractAttackSkillComponentBehaviour : AbstractCustomComponentBehaviour, ISkill, IAttackSkill
    {
        IAttackController _attackController;

        [SerializeField] protected GameObject _skillEffect;

        List<Hex> _curAttackRange;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISkill>(this);
            owner.RegisterInterface<IAttackSkill>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _attackController);
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

        void IAttackSkill.StartAttackEnable(List<Hex> attackRange, ICustomComponentCollection attackOrigin)
        {
            if (Owner.QueryInterface(out ISkillSetting skillSetting))
            {
                var attackSetting = new AttackSetting
                {
                    _power = skillSetting.Damage
                };
                _curAttackRange = attackRange;

                _attackController.StartAttack(_curAttackRange, attackSetting, attackOrigin);
            }
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
