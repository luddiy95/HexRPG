using UnityEngine;
using Zenject;
using UniRx;
using Cinemachine;

namespace HexRPG.Battle.Player
{
    public class PlayerLocomotionBehaviour : LocomotionBehaviour
    {
        ICharacterInput _characterInput;
        IBattleObservable _battleObservable;
        IMemberObservable _memberObservable;
        BattleData _battleData;
        // Camera
        int _cameraRotateStep = 0;
        int _cameraRotateStepMax;
        int _cameraRotateUnit;
        int _cameraAngle = 0;
        CinemachineVirtualCamera _mainVirtualCamera;
        CinemachineOrbitalTransposer _cameraTransposer;

        [Inject]
        public void Construct(
            ICharacterInput characterInput,
            IBattleObservable battleObservable,
            ITransformController transformController,
            IMemberObservable memberObservable,
            BattleData battleData
        )
        {
            _characterInput = characterInput;
            _battleObservable = battleObservable;
            _transformController = transformController;
            _memberObservable = memberObservable;
            _battleData = battleData;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(member =>
                {
                    //TODO: _speedÇmemberÇ≤Ç∆ÇÃspeedÇ…Ç∑ÇÈ
                    _colliderRadius = member.ColliderController.Collider.radius;
                })
                .AddTo(this);

            // Camera
            _cameraRotateUnit = _battleData.CameraRotateUnit;
            _cameraRotateStepMax = 360 / _cameraRotateUnit;
            _mainVirtualCamera = _battleObservable.MainVirtualCamera;
            _cameraTransposer = _mainVirtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();

            _characterInput.CameraRotateDir
                .Subscribe(dir =>
                {
                    _cameraRotateStep += dir;
                    _cameraRotateStep %= (int)Mathf.Sign(_cameraRotateStep) * _cameraRotateStepMax;
                    _cameraAngle = _cameraRotateStep * _cameraRotateUnit;

                    _transformController.DefaultRotation = _cameraAngle;
                    _transformController.RotationAngle = 0;

                    _cameraTransposer.m_Heading.m_Bias = _cameraAngle;
                    var cameraRotationCache = _mainVirtualCamera.transform.rotation.eulerAngles;
                    _mainVirtualCamera.transform.rotation = Quaternion.Euler(new Vector3(cameraRotationCache.x, _cameraAngle, cameraRotationCache.z));
                })
                .AddTo(this);
        }

        protected override void SetSpeed(Vector3 direction, float speed)
        {
            // directionÇÃêÊÇ…êiÇﬂÇÈÇ©Ç«Ç§Ç©
            var worldDirection = (Quaternion.AngleAxis(_cameraAngle, Vector3.up) * direction).normalized;
            var directionHex = TransformExtensions.GetLandedHex(_transformController.Position + worldDirection * _colliderRadius);

            if (directionHex != null && directionHex.IsPlayerHex) Rigidbody.velocity = worldDirection * (float)speed;
            else Rigidbody.velocity = Vector3.zero;
        }
    }
}
