using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Player
{
    public interface IInputEventProvider
    {
        IReadOnlyReactiveProperty<Vector3> TouchPosition { get; }
    }
}
