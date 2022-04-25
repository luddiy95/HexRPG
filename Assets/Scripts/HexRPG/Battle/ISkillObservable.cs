using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillObservable
    {
        IReadOnlyReactiveProperty<Hex[]> OnSkillAttack { get; }
        IObservable<Unit> OnFinishSkill { get; }
    }
}