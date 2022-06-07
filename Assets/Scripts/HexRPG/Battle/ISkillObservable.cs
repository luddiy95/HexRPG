using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillObservable
    {
        IObservable<Unit> OnStartReservation { get; }
        IObservable<Unit> OnFinishReservation { get; }

        IObservable<SkillAttackSetting> OnSkillAttackEnable { get; }
        IObservable<Unit> OnSkillAttackDisable { get; }

        IObservable<Hex[]> OnSkillAttack { get; }

        IObservable<Unit> OnFinishSkill { get; }
    }
}