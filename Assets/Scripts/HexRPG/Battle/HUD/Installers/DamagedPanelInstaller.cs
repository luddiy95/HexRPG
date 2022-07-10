using UnityEngine;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class DamagedPanelInstaller : MonoInstaller
    {
        [SerializeField] GameObject _damagedDisplayPrefab;
        [SerializeField] Transform _damagedDisplayRoot;

        public override void InstallBindings()
        {
            Container.BindFactory<DamagedDisplayHUD, DamagedDisplayHUD.Factory>()
                .FromPoolableMemoryPool<DamagedDisplayHUD, DamagedDisplayHUD.Pool>(pool => pool
                    .WithInitialSize(3)
                    .FromComponentInNewPrefab(_damagedDisplayPrefab).UnderTransform(_damagedDisplayRoot));
        }
    }
}
