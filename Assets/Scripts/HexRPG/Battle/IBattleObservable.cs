using System;
using UniRx;

namespace HexRPG.Battle
{
    using Player;
    using Stage;

    public interface IBattleObservable
    {
        /// <summary>
        /// ������CustomComponentCollection�ɑ΂��Ĕ��s
        /// </summary>
        IObservable<IPlayerComponentCollection> OnPlayerSpawn { get; }
        IObservable<ICustomComponentCollection> OnEnemySpawn { get; }

        /// <summary>
        /// ��������ׂ�CustomComponentCollection���S�Đ������ꂽ�甭�s
        /// </summary>
        IObservable<Unit> OnBattleStart { get; }

        Hex PlayerLandedHex { get; }
    }
}
