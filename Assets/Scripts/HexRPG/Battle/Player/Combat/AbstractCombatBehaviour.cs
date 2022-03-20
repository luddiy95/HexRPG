using UnityEngine;
using UnityEngine.Playables;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player.Combat
{
    public class AbstractCombatBehaviour : MonoBehaviour, ICombat, ICombatObservable
    {
        IAttackController _attackController;
        IAttackApplicator _attackApplicator;
        ICombatSetting _combatSetting;

        [SerializeField] protected PlayableDirector _director;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        [SerializeField] AttackCollider[] _attackColliders;

        ICharacterComponentCollection _combatOrigin;

        [Inject]
        public void Construct(
            IAttackController attackController,
            IAttackApplicator attackApplicator,
            ICombatSetting combatSetting
        )
        {
            _attackController = attackController;
            _attackApplicator = attackApplicator;
            _combatSetting = combatSetting;
        }

        void ICombat.Init(PlayableAsset timeline, ICharacterComponentCollection combatOrigin, Animator animator)
        {
            Array.ForEach(_attackColliders, attackCollider => attackCollider.AttackApplicator = _attackApplicator);

            //_skillEffect.SetActive(false);
            _combatOrigin = combatOrigin;

            _director.playableAsset = timeline;
            foreach (var bind in _director.playableAsset.outputs)
            {
                if (bind.streamName == "Animation Track")
                {
                    _director.SetGenericBinding(bind.sourceObject, animator);
                }
            }
            _director.stopped += ((obj) => _onFinishCombat.OnNext(Unit.Default));
        }

        void ICombat.Execute()
        {
            _director.Play();
        }

        public virtual void StartAttackEnable()
        {
            var attackSetting = new CombatAttackSetting
            {
                _power = _combatSetting.Damage
            };
            _attackController.StartAttack(attackSetting, _combatOrigin);
        }

        public virtual void FinishAttackEnable()
        {
            _attackController.FinishAttack();
        }
    }
}
