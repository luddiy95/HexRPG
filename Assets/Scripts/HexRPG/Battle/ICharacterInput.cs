using System;
using UniRx;
using UnityEngine;

namespace HexRPG.Battle
{
    public interface ICharacterInput
    {
        IReadOnlyReactiveProperty<Vector3> Direction { get; }

        IReadOnlyReactiveProperty<int> SelectedSkillIndex { get; }

        IObservable<Unit> OnSkillDecide { get; }
        IObservable<Unit> OnSkillCancel { get; }

        IReadOnlyReactiveProperty<int> CameraRotateDir { get; }
    }
}
