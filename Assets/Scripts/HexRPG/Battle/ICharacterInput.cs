using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;
    public interface ICharacterInput
    {
        IReadOnlyReactiveProperty<Hex> Destination { get; }

        IObservable<Unit> OnFire { get; }
    }
}
