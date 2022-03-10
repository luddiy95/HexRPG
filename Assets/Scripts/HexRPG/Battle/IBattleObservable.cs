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

        List<IEnemyComponentCollection> EnemyList { get; }

        CinemachineBrain CinemachineBrain { get; }
        CinemachineVirtualCamera MainVirtualCamera { get; }
    }
}
