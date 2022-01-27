using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface IBattleObservable : IFeature
    {
        /// <summary>
        /// 直おきCustomComponentCollectionに対して発行
        /// </summary>
        IObservable<ICustomComponentCollection> OnPlayerSpawn { get; }
        IObservable<ICustomComponentCollection> OnEnemySpawn { get; }

        /// <summary>
        /// 生成するべきCustomComponentCollectionが全て生成されたら発行
        /// </summary>
        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }
    }
}
