using UnityEngine;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Enemy
{
    public interface IEnemyComponentCollection : ICharacterComponentCollection
    {
        ISkillSpawnObservable SkillSpawnObservable { get; }
        IHealth Health { get; }

        //TODO: Decorator�p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class EnemyOwner : MonoBehaviour, IEnemyComponentCollection
    {
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] ISkillSpawnObservable IEnemyComponentCollection.SkillSpawnObservable { get; }
        [Inject] IHealth IEnemyComponentCollection.Health { get; }

        //TODO: Decorator�p
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