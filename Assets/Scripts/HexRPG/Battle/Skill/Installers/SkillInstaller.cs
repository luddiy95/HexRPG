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
        }
    }
}
