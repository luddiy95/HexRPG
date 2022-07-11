using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;
using Cinemachine;

namespace HexRPG.Battle.Player
{
    public interface ICameraRotateController
    {
        void FixTimeCameraRotate(int rotateDir, float rotateTime);
    }

    public interface ICameraRotateObservable
    {
        bool IsCameraRotating { get; }
    }

    public class PlayerCameraController : ICameraRotateController, ICameraRotateObservable, IInitializable, IDisposable
    {
        IBattleObservable _battleObservable;
        BattleData _battleData;

        bool ICameraRotateObservable.IsCameraRotating => (_cts != null);

        int _cameraRotateStep = 0;
        int _cameraRotateStepMax;
        int _cameraRotateUnit;
        int _goalCameraBias = 0;
        CinemachineVirtualCamera _mainVirtualCamera;
        CinemachineOrbitalTransposer _cameraTransposer;

        CancellationTokenSource _cts;

        public PlayerCameraController(
            IBattleObservable battleObservable,
            BattleData battleData
        )
        {
            _battleObservable = battleObservable;
            _battleData = battleData;
        }

        void IInitializable.Initialize()
        {
            _cameraRotateUnit = _battleData.cameraRotateUnit;
            _cameraRotateStepMax = 360 / _cameraRotateUnit;
            _mainVirtualCamera = _battleObservable.MainVirtualCamera;
            _cameraTransposer = _battleObservable.CameraTransposer;
        }

        void ICameraRotateController.FixTimeCameraRotate(int rotateDir, float rotateTime)
        {
            _cameraRotateStep += rotateDir;
            _goalCameraBias = _cameraRotateStep * _cameraRotateUnit;

            InternalFixTimeCameraRotate(rotateTime).Forget();
        }

        async UniTaskVoid InternalFixTimeCameraRotate(float rotateTime)
        {
            float waitTime = Time.timeSinceLevelLoad + rotateTime;
            var startBias = _cameraTransposer.m_Heading.m_Bias;
            var rotateAmount = _goalCameraBias - startBias;


            TokenCancel();
            _cts = new CancellationTokenSource();

            await UniTask.WaitWhile(() =>
            {
                var diff = waitTime - Time.timeSinceLevelLoad;
                if (diff <= 0)
                {
                    return false;
                }
                else
                {
                    var diffAngle = rotateAmount * (rotateTime - diff) / rotateTime;
                    var cameraBias = (int)(startBias + diffAngle);
                    SetCameraBias(cameraBias);
                    return true;
                }
            }, cancellationToken: _cts.Token);

            SetCameraBias(_goalCameraBias);
            TokenCancel();
        }

        void SetCameraBias(int bias)
        {
            _cameraTransposer.m_Heading.m_Bias = bias;
            var cameraRotationCache = _mainVirtualCamera.transform.rotation.eulerAngles;
            _mainVirtualCamera.transform.rotation = Quaternion.Euler(new Vector3(cameraRotationCache.x, bias, cameraRotationCache.z));
        }

        void TokenCancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        void IDisposable.Dispose()
        {
            TokenCancel();
        }
    }
}
