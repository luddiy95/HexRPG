using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    public interface ICombatObservable
    {
        IObservable<Unit> OnFinishCombat { get; }
    }
}
