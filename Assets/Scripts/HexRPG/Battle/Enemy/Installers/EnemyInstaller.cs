using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public class EnemyInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<ActionStateController>().AsSingle();
            Container.BindInterfacesTo<EnemyMover>().AsSingle();
            Container.BindInterfacesTo<EnemyTurnToPlayer>().AsSingle();

            Container.BindInterfacesTo<Health>().AsSingle();
        }
    }
}
