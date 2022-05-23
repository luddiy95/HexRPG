using System;
using System.Linq;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    using Stage;
    using Enemy;

    public interface ILiberateController
    {
        void Liberate(Hex[] hexList);
    }

    public interface ILiberateObservable
    {
        IObservable<Hex[]> SuccessLiberateHexList { get; }
    }

    public class Liberater : ILiberateController, ILiberateObservable, IInitializable
    {
        ICharacterComponentCollection _owner;

        IObservable<Hex[]> ILiberateObservable.SuccessLiberateHexList => _successLiberateHexList;
        readonly ISubject<Hex[]> _successLiberateHexList = new Subject<Hex[]>();

        bool _isPlayer = true;

        public Liberater(
            ICharacterComponentCollection owner
        )
        {
            _owner = owner;
        }

        void IInitializable.Initialize()
        {
            _isPlayer = !(_owner is IEnemyComponentCollection);
        }

        void ILiberateController.Liberate(Hex[] hexList)
        {
            _successLiberateHexList.OnNext(hexList.Where(hex => hex.Liberate(_isPlayer)).ToArray());
        }
    }
}
