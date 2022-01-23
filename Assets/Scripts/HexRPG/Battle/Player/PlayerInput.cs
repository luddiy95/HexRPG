using UnityEngine;
using System;
using UniRx;
using UniRx.Triggers;

namespace HexRPG.Battle.Player
{
    using Battle.Stage;

    public class PlayerInput : AbstractCustomComponentBehaviour, ICharacterInput
    {
        [Header("Œˆ’èƒ{ƒ^ƒ“")]
        [SerializeField] GameObject _btnFire;

        IReadOnlyReactiveProperty<Hex> ICharacterInput.Destination => _destination;
        ReactiveProperty<Hex> _destination = new ReactiveProperty<Hex>();

        IObservable<Unit> ICharacterInput.OnFire => _onFire;
        ISubject<Unit> _onFire = new Subject<Unit>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ICharacterInput>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.QueryInterface(out IUpdateObservable update) == true)
            {
                update.OnUpdate((int)UPDATE_ORDER.INPUT)
                    .Subscribe(_ =>
                    {
                        UpdateDestination();
                    }).AddTo(this);
            }

            ObservablePointerClickTrigger trigger;
            if (!_btnFire.TryGetComponent(out trigger)) trigger = _btnFire.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    _onFire.OnNext(Unit.Default);
                })
                .AddTo(this);
        }

        void UpdateDestination()
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Physics.Raycast(ray, out var hit, Mathf.Infinity, LayerMask.GetMask("HexMoveableIndicator"));
                if (hit.collider == null) return;
                Hex hex = TransformExtensions.GetLandedHex(hit.transform.position);
                if (hex == null) return;
                _destination.SetValueAndForceNotify(hex);
            }
        }
    }
}
