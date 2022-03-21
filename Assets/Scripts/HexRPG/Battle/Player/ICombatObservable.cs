using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    public interface ICombatObservable
    {
        IObservable<Unit> OnFinishCombat { get; }
        IReadOnlyReactiveProperty<Vector3> Velocity { get; }
    }
}
