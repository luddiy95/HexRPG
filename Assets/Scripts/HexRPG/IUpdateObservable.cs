using UniRx;
using System;

namespace HexRPG
{
    public interface IUpdateObservable
    {
        IObservable<Unit> OnUpdate(int updateOrder);
    }
}
