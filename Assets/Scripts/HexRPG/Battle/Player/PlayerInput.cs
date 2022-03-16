using UnityEngine;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Battle.Stage;
    using Battle.UI;

    public class PlayerInput : MonoBehaviour, ICharacterInput
    {
        IUpdateObservable _updateObservable;

        [Header("決定ボタン")]
        [SerializeField] GameObject _btnFire;

        [Header("カメラ回転ボタン")]
        [SerializeField] Transform _cameraRotLeft;
        [SerializeField] Transform _cameraRotRight;

        IReadOnlyReactiveProperty<Vector3> ICharacterInput.Direction => _direction;
        ReactiveProperty<Vector3> _direction = new ReactiveProperty<Vector3>();

        IObservable<Unit> ICharacterInput.OnFire => _onFire;
        ISubject<Unit> _onFire = new Subject<Unit>();

        IReadOnlyReactiveProperty<int> ICharacterInput.CameraRotateDir => _cameraRotateDir;
        ReactiveProperty<int> _cameraRotateDir = new ReactiveProperty<int>();

        [Inject]
        public void Construct(IUpdateObservable updateObservable)
        {
            _updateObservable = updateObservable;
        }

        void Start()
        {
            var isBtnFireClicked = false;
            int cameraRotateDir = 0;

            _updateObservable.OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    if (isBtnFireClicked)
                    {
                        _onFire.OnNext(Unit.Default);
                        isBtnFireClicked = false;
                    }

                    if(cameraRotateDir != 0)
                    {
                        _cameraRotateDir.SetValueAndForceNotify(cameraRotateDir);
                        cameraRotateDir = 0;
                    }

                    UpdateDirection();
                }).AddTo(this);

            _btnFire.OnClickListener(() =>
            {
                isBtnFireClicked = true;
            }, gameObject);

            _cameraRotLeft.GetChild(2).gameObject.OnClickListener(() =>
            {
                cameraRotateDir = +1;
            }, gameObject);

            _cameraRotRight.GetChild(2).gameObject.OnClickListener(() =>
            {
                cameraRotateDir = -1;
            }, gameObject);
        }

        void UpdateDirection()
        {
            _direction.Value = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        }
    }
}
