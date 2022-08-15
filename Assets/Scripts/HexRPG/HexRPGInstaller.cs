using UnityEngine;
using Zenject;

namespace HexRPG
{
    using Battle;

    public class HexRPGInstaller : MonoInstaller
    {
        [SerializeField] BattleData _battleData;

        public override void InstallBindings()
        {
            Container.Bind<BattleData>().FromInstance(_battleData);
        }
    }
}
