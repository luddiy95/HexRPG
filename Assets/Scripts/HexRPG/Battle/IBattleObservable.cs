using System;
using UniRx;

namespace HexRPG.Battle
{
    using Player;
    using Enemy;
    using Stage;

    public interface IBattleObservable
    {
        IObservable<IPlayerComponentCollection> OnPlayerSpawn { get; }
        IObservable<IEnemyComponentCollection> OnEnemySpawn { get; }

        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }
    }
}
