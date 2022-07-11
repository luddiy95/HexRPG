using System.Collections.Generic;
using System;
using UniRx;
using Cinemachine;

namespace HexRPG.Battle
{
    using Player;
    using Enemy;
    using Stage;

    public interface IBattleObservable
    {
        IReadOnlyReactiveProperty<IPlayerComponentCollection> OnPlayerSpawn { get; }
        IObservable<IEnemyComponentCollection> OnEnemySpawn { get; }

        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }

        IReadOnlyReactiveCollection<IEnemyComponentCollection> EnemyList { get; }
        IEnumerable<Hex> EnemyDestinationHexList { get; } // 一度copyされてから使う想定

        CinemachineBrain CinemachineBrain { get; }
        CinemachineVirtualCamera MainVirtualCamera { get; }
        CinemachineOrbitalTransposer CameraTransposer { get; }
    }
}
