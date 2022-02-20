using Zenject;
using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public class EnemyInstaller : MonoInstaller
    {
        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EnemyOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<ActionStateController>().AsSingle();
            Container.BindInterfacesTo<EnemyMover>().AsSingle();
            Container.BindInterfacesTo<EnemyTurnToPlayer>().AsSingle();

            Container.BindInterfacesTo<DamageApplicable>().AsSingle();

            Container.BindInterfacesTo<Health>().AsSingle();
        }
    }
}
