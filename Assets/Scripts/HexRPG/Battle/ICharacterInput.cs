using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;
    public interface ICharacterInput : IFeature
    {
        IReadOnlyReactiveProperty<Hex> Destination { get; }

        IObservable<Unit> OnFire { get; }
    }
}
