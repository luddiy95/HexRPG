using System;
using UniRx;

namespace HexRPG.Battle.UI
{
    public interface ICharacterUI
    {
        void Bind(ICharacterComponentCollection character);

        IObservable<Unit> OnBack { get; }
        void SwitchOperation(bool inOperation); // ���쒆���ǂ���
    }
}