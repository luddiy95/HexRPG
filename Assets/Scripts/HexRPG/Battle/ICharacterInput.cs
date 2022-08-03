using System;
using UniRx;
using UnityEngine;

namespace HexRPG.Battle
{
    public interface ICharacterInput
    {
        Vector3 Direction { get; }

        IObservable<int> CameraRotateDir { get; }

        IObservable<Unit> OnCombat { get; }

        IReadOnlyReactiveProperty<int> SelectedSkillIndex { get; }

        IObservable<Unit> OnSkillDecide { get; }
        IObservable<Unit> OnSkillCancel { get; }

        IObservable<Unit> OnAppendSkill { get; }

        IObservable<int> SelectedMemberIndex { get; }
    }
}
