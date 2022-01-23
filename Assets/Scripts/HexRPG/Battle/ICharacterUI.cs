using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface ICharacterUI : IFeature
    {
        void Bind(ICustomComponentCollection character);

        IObservable<Unit> OnBack { get; }
        void SwitchShow(bool isShow);
    }
}