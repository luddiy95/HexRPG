using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public class Fire : AbstractCustomComponentBehaviour, ISkill
    {
        [SerializeField] protected GameObject _skillEffect;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISkill>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
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

        void ISkill.OnFinishEffect()
        {
            _skillEffect.SetActive(false);
        }
    }
}
