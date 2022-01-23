using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface ISkillObservable : IFeature
    {
        IObservable<Unit> OnStartSkill { get; }
        IObservable<Unit> OnFinishSkill { get; }
    }
}