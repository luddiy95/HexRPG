using Zenject;
using UnityEngine;

namespace HexRPG.Battle.Skill
{
    public class SkillInstaller : MonoInstaller
    {
        [Inject] Transform _spawnRoot;
        [Inject] Vector3 _spawnPos;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SkillOwner>().FromComponentOnRoot();
            Container.BindInstance(_spawnRoot).WhenInjectedInto<TransformBehaviour>();
            Container.BindInstance(_spawnPos).WhenInjectedInto<TransformBehaviour>();

            Container.BindInterfacesTo<AttackController>().AsSingle();
        }
    }
}
