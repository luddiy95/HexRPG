using System.Collections.Generic;
using System;
using UniRx;

namespace HexRPG.Battle
{
    public enum UPDATE_ORDER
    {
        INPUT,

        DAMAGED,

        MOVE,

        ACTION_TRANSITION,

        CAMERA,

        TIME,
    }

    public class UpdateFeature : IUpdateObservable, IUpdater
    {
        readonly SortedDictionary<int, ISubject<Unit>> _updateStreams = new SortedDictionary<int, ISubject<Unit>>();

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
