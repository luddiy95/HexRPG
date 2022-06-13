using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface ICombatObservable
    {
        IObservable<CombatAttackSetting> OnCombatAttackEnable { get; }
        IObservable<Unit> OnCombatAttackDisable { get; }

        IObservable<Unit> OnFinishCombat { get; }
    }
}
