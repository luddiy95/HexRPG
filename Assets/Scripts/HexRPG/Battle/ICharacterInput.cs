using System;
using UniRx;
using UnityEngine;

namespace HexRPG.Battle
{
    public interface ICharacterInput
    {
        IReadOnlyReactiveProperty<Vector3> Direction { get; }

        IObservable<Unit> OnCombat { get; }

        IReadOnlyReactiveProperty<int> SelectedSkillIndex { get; }

        IObservable<Unit> OnSkillDecide { get; }
        IObservable<Unit> OnSkillCancel { get; }

        IObservable<int> CameraRotateDir { get; }
    }
}
