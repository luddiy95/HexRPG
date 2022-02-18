using UnityEngine;
using System;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Battle.Stage;

    public class PlayerInput : MonoBehaviour, ICharacterInput
    {
        IUpdateObservable _updateObservable;

        [Header("Œˆ’èƒ{ƒ^ƒ“")]
        [SerializeField] GameObject _btnFire;

        IReadOnlyReactiveProperty<Hex> ICharacterInput.Destination => _destination;
        ReactiveProperty<Hex> _destination = new ReactiveProperty<Hex>();

        IObservable<Unit> ICharacterInput.OnFire => _onFire;
        ISubject<Unit> _onFire = new Subject<Unit>();

        [Inject]
        public void Construct(IUpdateObservable updateObservable)
        {
            _updateObservable = updateObservable;
        }

        void Start()
        {
            var isBtnFireClicked = false;

            _updateObservable.OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    if (isBtnFireClicked)
                    {
                        _onFire.OnNext(Unit.Default);
                        isBtnFireClicked = false;
                    }
                    UpdateDestination();
                }).AddTo(this);

            ObservablePointerClickTrigger trigger;
            if (!_btnFire.TryGetComponent(out trigger)) trigger = _btnFire.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    isBtnFireClicked = true;
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
