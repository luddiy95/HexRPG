using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface IBattleObservable : IFeature
    {
        /// <summary>
        /// ������CustomComponentCollection�ɑ΂��Ĕ��s
        /// </summary>
        IObservable<ICustomComponentCollection> OnPlayerSpawn { get; }
        IObservable<ICustomComponentCollection> OnEnemySpawn { get; }

        /// <summary>
        /// ��������ׂ�CustomComponentCollection���S�Đ������ꂽ�甭�s
        /// </summary>
        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }
    }
}
