using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UniRx;
using Cinemachine;

namespace HexRPG.Battle
{
    public interface ILocomotionController
    {
        void SetSpeed(Vector3 direction);
        void Stop();

        void SnapHexCenter();
    }

    public class LocomotionBehaviour : MonoBehaviour, ILocomotionController
    {
        ICharacterInput _characterInput;
        IBattleObservable _battleObservable;
        ITransformController _transformController;

        [Header("������Rigidbody�Bnull�Ȃ炱�̃I�u�W�F�N�g")]
        [SerializeField] Rigidbody _rigidbody;
        //TODO: Rigidbody(Player)�̎q�I�u�W�F�N�g��Member��MeshCollider(Convex)�����Ȃ��ƏՓ˔��肵�Ă���Ȃ�)

        int _cameraRotateStep = 0;
        int _cameraAngle = 0;
        CinemachineVirtualCamera _mainVirtualCamera;
        CinemachineOrbitalTransposer _cameraTransposer;
        //TODO: speed��setting����Ƃ��Ă���
        float _speed = 5f;

        [Inject]
        public void Construct(
            ICharacterInput characterInput,
            IBattleObservable battleObservable,
            ITransformController transformController
        )
        {
            _characterInput = characterInput;
            _battleObservable = battleObservable;
            _transformController = transformController;
        }

        void Start()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            _mainVirtualCamera = _battleObservable.MainVirtualCamera;
            _cameraTransposer = _mainVirtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();

            _characterInput.CameraRotateDir
                .Subscribe(dir =>
                {
                    _cameraRotateStep += dir;
                    _cameraAngle = _cameraRotateStep * 30;

                    _transformController.DefaultRotation = _cameraAngle;
                    _transformController.RotationAngle = 0;

                    _cameraTransposer.m_Heading.m_Bias = _cameraAngle;
                    var cameraRotationCache = _mainVirtualCamera.transform.rotation.eulerAngles;
                    _mainVirtualCamera.transform.rotation = Quaternion.Euler(new Vector3(cameraRotationCache.x, _cameraAngle, cameraRotationCache.z));
                })
                .AddTo(this);
        }

        void ILocomotionController.SetSpeed(Vector3 direction)
        {
            _rigidbody.velocity = (Quaternion.AngleAxis(_cameraAngle, Vector3.up) * direction).normalized * _speed;
        }

        void ILocomotionController.Stop()
        {
            _rigidbody.velocity = Vector3.zero;
        }

        void ILocomotionController.SnapHexCenter()
        {
            _transformController.Position = _transformController.GetLandedHex().transform.position;
        }
    }
}
