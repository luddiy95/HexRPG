using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Triggers;
using UniRx;

namespace HexRPG.Battle.Skill
{
    public abstract class BaseSkill : MonoBehaviour
    {
        [SerializeField] Sprite _icon;
        public Sprite Icon => _icon;
        [SerializeField] int _MPcost;
        public ref readonly int MPcost => ref _MPcost;
        [SerializeField] int _damage;
        public ref readonly int Damage => ref _damage;
        [SerializeField] List<Vector2> _range;
        public List<Vector2> Range => _range;
        [SerializeField] string _skillAnimationParam;
        public string SkillAnimationParam => _skillAnimationParam;
        [SerializeField] protected GameObject _skillEffect;

        protected virtual void Awake()
        {
            _skillEffect.SetActive(false);
        }

        public abstract void Init();

        public virtual void StartSkill()
        {
            _skillEffect.SetActive(false);
        }

        public virtual void FinishSkill()
        {
            _skillEffect.SetActive(false);
        }

        public virtual void StartEffect()
        {
            _skillEffect.SetActive(true);
        }

        public virtual void OnFinishEffect()
        {
            _skillEffect.SetActive(false);
        }
    }
}
