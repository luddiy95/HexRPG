using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface ISkillObservable
    {
        IObservable<Unit> OnFinishSkill { get; }
    }
}