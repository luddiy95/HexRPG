using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface ICombatObservable
    {
        IObservable<Unit> OnFinishCombat { get; }
    }
}
