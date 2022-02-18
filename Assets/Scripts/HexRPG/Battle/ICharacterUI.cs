using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface ICharacterUI
    {
        void Bind(ICharacterComponentCollection character);

        IObservable<Unit> OnBack { get; }
        void SwitchShow(bool isShow);
    }
}