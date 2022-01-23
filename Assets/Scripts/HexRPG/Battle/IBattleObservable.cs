using System;

namespace HexRPG.Battle
{
    public interface IBattleObservable : IFeature
    {
        IObservable<ICustomComponentCollection> OnPlayerSpawn { get; }
    }
}
