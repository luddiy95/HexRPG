using UnityEngine;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Enemy
{
    public interface IEnemyComponentCollection : ICharacterComponentCollection
    {
        IColliderController ColliderController { get; }
        IAnimationController AnimationController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class EnemyOwner : MonoBehaviour, IEnemyComponentCollection
    {
        [Inject] IColliderController IEnemyComponentCollection.ColliderController { get; }
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        [Inject] IAnimationController IEnemyComponentCollection.AnimationController { get; }
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IHealth ICharacterComponentCollection.Health { get; }
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

        public class Factory : PlaceholderFactory<Transform, Vector3, EnemyOwner>
        {

        }
    }
}
