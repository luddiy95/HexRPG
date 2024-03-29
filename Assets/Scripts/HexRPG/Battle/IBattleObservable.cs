using System.Collections.Generic;
using System;
using UniRx;
using Cinemachine;

namespace HexRPG.Battle
{
    using Player;
    using Enemy;
    using Stage;
    using Stage.Tower;

    public interface IBattleObservable
    {
        IReadOnlyReactiveProperty<IPlayerComponentCollection> OnPlayerSpawn { get; }
        IObservable<IEnemyComponentCollection> OnEnemySpawn { get; }

        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }

        IReadOnlyReactiveCollection<IEnemyComponentCollection> EnemyList { get; }
        IEnumerable<Hex> EnemyDestinationHexList { get; } // 一度copyされてから使う想定

        IObservable<ITowerComponentCollection> OnTowerInit { get; }
        List<ITowerComponentCollection> TowerList { get; }

        IObservable<Hex[]> OnReduceEnemyNavMesh { get; }
        IObservable<Unit> OnCompleteUpdateNavMesh { get; }

        IReadOnlyReactiveProperty<GameResultType> GameResultType { get; }

        CinemachineBrain CinemachineBrain { get; }
        CinemachineVirtualCamera MainVirtualCamera { get; }
        CinemachineOrbitalTransposer CameraTransposer { get; }
    }
}
