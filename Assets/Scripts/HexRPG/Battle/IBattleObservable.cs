using System;
using UniRx;

namespace HexRPG.Battle
{
    using Player;
    using Stage;

    public interface IBattleObservable
    {
        /// <summary>
        /// ’¼‚¨‚«CustomComponentCollection‚É‘Î‚µ‚Ä”­s
        /// </summary>
        IObservable<IPlayerComponentCollection> OnPlayerSpawn { get; }
        IObservable<ICustomComponentCollection> OnEnemySpawn { get; }

        /// <summary>
        /// ¶¬‚·‚é‚×‚«CustomComponentCollection‚ª‘S‚Ä¶¬‚³‚ê‚½‚ç”­s
        /// </summary>
        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }
    }
}
