using System.Collections.Generic;
using System;
using UniRx;

namespace HexRPG
{
    public enum UPDATE_ORDER
    {
        INPUT,

        COLLISION,

        MOVE,

        ACTION_TRANSITION,

        CAMERA,

        JUDGE,
    }

    public class UpdateFeature : AbstractCustomComponent, IUpdateObservable, IUpdater
    {
        readonly SortedDictionary<int, ISubject<Unit>> _updateStreams = new SortedDictionary<int, ISubject<Unit>>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IUpdateObservable>(this);
            owner.RegisterInterface<IUpdater>(this);
        }

        void IUpdater.FireUpdateStreams()
        {
            foreach (var pair in _updateStreams)
            {
                var update = pair.Value;
                update.OnNext(Unit.Default);
            }
        }

        IObservable<Unit> IUpdateObservable.OnUpdate(int updateOrder)
        {
            if (_updateStreams.TryGetValue(updateOrder, out var update))
            {
                return update;
            }
            else
            {
                var newStream = new Subject<Unit>();
                _updateStreams.Add(updateOrder, newStream);
                return newStream;
            }
        }
    }
}
