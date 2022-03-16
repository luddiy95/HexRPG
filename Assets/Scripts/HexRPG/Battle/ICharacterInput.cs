using System;
using UniRx;
using UnityEngine;

namespace HexRPG.Battle
{
    public interface ICharacterInput
    {
        IReadOnlyReactiveProperty<Vector3> Direction { get; }

        IObservable<Unit> OnFire { get; }

        IReadOnlyReactiveProperty<int> CameraRotateDir { get; }
    }
}
