using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Combat
{
    public class CombatInstaller : MonoInstaller
    {
        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<CombatOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();
        }
    }
}
