using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface IBattleObservable : IFeature
    {
        /// <summary>
        /// ’¼‚¨‚«CustomComponentCollection‚É‘Î‚µ‚Ä”­s
        /// </summary>
        IObservable<ICustomComponentCollection> OnPlayerSpawn { get; }
        IObservable<ICustomComponentCollection> OnEnemySpawn { get; }

        /// <summary>
        /// ¶¬‚·‚é‚×‚«CustomComponentCollection‚ª‘S‚Ä¶¬‚³‚ê‚½‚ç”­s
        /// </summary>
        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }
    }
}
