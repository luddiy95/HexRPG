using System.Collections.Generic;
using System;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISkillObservable
    {
        IObservable<Unit> OnStartReservation { get; }
        IObservable<Unit> OnFinishReservation { get; }

        IObservable<IEnumerable<Hex>> OnAttackEnable { get; }
        IObservable<HitData> OnAttackHit { get; }
        IObservable<IEnumerable<Hex>> OnAttackDisable { get; }

        IObservable<Unit> OnFinishSkill { get; }
    }
}