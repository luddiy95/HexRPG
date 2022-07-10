using UnityEngine;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class ScorePanelInstaller : MonoInstaller
    {
        [SerializeField] GameObject _acquiredMessagePrefab;
        [SerializeField] Transform _acquiredMessageRoot;

        const int _maxShowMessageCount = 5;

        public override void InstallBindings()
        {
            Container.BindFactory<AcquiredMessageHUD, AcquiredMessageHUD.Factory>()
                .FromPoolableMemoryPool<AcquiredMessageHUD, AcquiredMessageHUD.Pool>(pool => pool
                    .WithFixedSize(_maxShowMessageCount)
                    .FromComponentInNewPrefab(_acquiredMessagePrefab)
                    .UnderTransform(_acquiredMessageRoot));
        }
    }
}
