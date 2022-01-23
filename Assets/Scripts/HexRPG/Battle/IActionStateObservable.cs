using System;
using System.Collections.Generic;
using UniRx;

namespace HexRPG.Battle
{
    public interface IActionStateObservable : IFeature
    {
        IReadOnlyReactiveProperty<ActionState> CurrentState { get; }

        IObservable<T> OnStart<T>() where T : ActionEvent<T>;

        IObservable<T> OnEnd<T>() where T : ActionEvent<T>;

        IReadOnlyReactiveProperty<Command> ExecutedCommand { get; }

        IObservable<ActionState> OnEnterState { get; }

        IObservable<ActionState> OnExitState { get; }

        ICollection<ActionState> StateHistory { get; }
    }
}
