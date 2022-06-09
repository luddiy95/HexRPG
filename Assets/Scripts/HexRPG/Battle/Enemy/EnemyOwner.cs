using UnityEngine;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Enemy
{
    public interface IEnemyComponentCollection : IAttackComponentCollection
    {
        IAnimationController AnimationController { get; }
        ICharacterActionStateController CharacterActionStateController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class EnemyOwner : MonoBehaviour, IEnemyComponentCollection
    {
        [Inject] IProfileSetting ICharacterComponentCollection.ProfileSetting { get; }
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IHealth ICharacterComponentCollection.Health { get; }

        [Inject] IAttackController IAttackComponentCollection.AttackController { get; }
        [Inject] IAttackObservable IAttackComponentCollection.AttackObservable { get; }
        [Inject] IDamageApplicable IAttackComponentCollection.DamageApplicable { get; }

        [Inject] IAnimationController IEnemyComponentCollection.AnimationController { get; }
        [Inject] ICharacterActionStateController IEnemyComponentCollection.CharacterActionStateController { get; }
        [Inject] ISkillSpawnObservable IEnemyComponentCollection.SkillSpawnObservable { get; }

        //TODO: Decorator—p
        [Inject] IActionStateObservable IEnemyComponentCollection.ActionStateObservable { get; }

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            IActiveController activeController
        )
        {
            battleObservable.OnBattleStart
                .Subscribe(_ => {
                    activeController.SetActive(true);
                })
                .AddTo(this);
        }

        void Start()
        {
            (this as ICharacterComponentCollection).DieObservable.OnFinishDie
                .Subscribe(_ => DestroyImmediate(gameObject))
                .AddTo(this);
        }

        public class Factory : PlaceholderFactory<Transform, Vector3, EnemyOwner>
        {

        }
    }
}
