using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player.InputImpl
{
    public sealed class InputEventProviderImpl : MonoBehaviour, IInputEventProvider
    {
        public IReadOnlyReactiveProperty<Vector3> TouchPosition => _touchPosition;
        readonly ReactiveProperty<Vector3> _touchPosition = new ReactiveProperty<Vector3>();

        void Awake()
        {
            _touchPosition.AddTo(this);

            this.UpdateAsObservable()
                .Where(_ => Input.GetMouseButton(0))
                .Subscribe(_ =>
                {
                    _touchPosition.Value = Input.mousePosition;
                })
                .AddTo(this);
        }
    }
}
