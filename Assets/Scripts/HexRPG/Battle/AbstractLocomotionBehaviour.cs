using UnityEngine;
using UniRx;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle
{
    public interface ILocomotionController
    {
        void SetSpeed(Vector3 direction, float? speed = null);
        void Stop();

        // rotateAngleだけ回転してほしい
        void Rotate(int rotateAngle, float eulerVelocity);
        void FixTimeRotate(int rotateAngle, float rotateTime);
        void ForceRotate(int goalRotateAngle);

        // posの方へ向くように回転してほしい
        int LookRotate(Vector3 pos, float eulerVelocity); // 現在の回転を中断するかどうか
        int LookRotate60(Vector3 pos, float eulerVelocity);
        int FixTimeLookRotate(Vector3 pos, float rotateTime);
        int ForceLookRotate(Vector3 pos);
        int ForceLookRotate60(Vector3 pos);

        void StopRotate();

        void SnapHexCenter();
    }

    public interface ILocomotionObservable
    {
        float StepDistanceByFrame { get; } //! 1Fで進む距離
        IObservable<Unit> OnFinishRotate { get; } //! 割り込みされることなく回転できたときのみ発行
    }

    public abstract class AbstractLocomotionBehaviour : MonoBehaviour, ILocomotionController, ILocomotionObservable
    {
        protected ITransformController _transformController;

        protected Rigidbody Rigidbody => _rigidbody ? _rigidbody : _rigidbody = GetComponent<Rigidbody>();
        [Header("動かすRigidbody。nullならこのオブジェクト")]
        [SerializeField] Rigidbody _rigidbody;

        public float StepDistanceByFrame => _speed * Time.fixedDeltaTime;

        IObservable<Unit> ILocomotionObservable.OnFinishRotate => _onFinishRotate;
        readonly ISubject<Unit> _onFinishRotate = new Subject<Unit>();

        protected float _speed = 7f;

        CancellationTokenSource _cts = null;

        void Start()
        {
            Initialize();
        }

        void ILocomotionController.Rotate(int rotateAngle, float eulerVelocity)
        {
            InternalRotate(rotateAngle, Mathf.Abs(rotateAngle) / eulerVelocity).Forget();
        }

        void ILocomotionController.FixTimeRotate(int rotateAngle, float rotateTime)
        {
            InternalRotate(rotateAngle, rotateTime).Forget();
        }

        int ILocomotionController.LookRotate(Vector3 pos, float eulerVelocity)
        {
            var rotateAngle = _transformController.GetLookRotationAngleY(pos) - _transformController.RotationAngle;
            rotateAngle = MathUtility.GetIntegerEuler(rotateAngle);
            InternalRotate(rotateAngle, Mathf.Abs(rotateAngle) / eulerVelocity).Forget();
            return rotateAngle;
        }

        int ILocomotionController.LookRotate60(Vector3 pos, float eulerVelocity)
        {
            var rotateAngle = MathUtility.GetIntegerEuler60(_transformController.GetLookRotationAngleY(pos)) - _transformController.RotationAngle;
            rotateAngle = MathUtility.GetIntegerEuler(rotateAngle);
            InternalRotate(rotateAngle, Mathf.Abs(rotateAngle) / eulerVelocity).Forget();
            return rotateAngle;
        }

        int ILocomotionController.FixTimeLookRotate(Vector3 pos, float rotateTime)
        {
            var rotateAngle = _transformController.GetLookRotationAngleY(pos) - _transformController.RotationAngle;
            rotateAngle = MathUtility.GetIntegerEuler(rotateAngle);
            InternalRotate(rotateAngle, rotateTime).Forget();
            return rotateAngle;
        }

        async UniTaskVoid InternalRotate(int rotateAngle, float rotateTime)
        {
            float waitTime = Time.timeSinceLevelLoad + rotateTime;
            var startAngleY = _transformController.RotationAngle;

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
                    var diffAngle = rotateAngle * (rotateTime - diff) / rotateTime;
                    _transformController.RotationAngle = (int)(startAngleY + diffAngle);
                    return true;
                }
            }, cancellationToken: _cts.Token);

            _transformController.RotationAngle = startAngleY + rotateAngle;
            TokenCancel();
            _onFinishRotate.OnNext(Unit.Default);
        }

        void ILocomotionController.ForceRotate(int goalRotateAngle)
        {
            _transformController.RotationAngle = goalRotateAngle;
        }

        int ILocomotionController.ForceLookRotate(Vector3 pos)
        {
            var rotateAngle = _transformController.GetLookRotationAngleY(pos) - _transformController.RotationAngle;
            rotateAngle = MathUtility.GetIntegerEuler(rotateAngle);
            _transformController.RotationAngle += rotateAngle;
            return rotateAngle;
        }

        int ILocomotionController.ForceLookRotate60(Vector3 pos)
        {
            var rotateAngle = MathUtility.GetIntegerEuler60(_transformController.GetLookRotationAngleY(pos)) - _transformController.RotationAngle;
            rotateAngle = MathUtility.GetIntegerEuler(rotateAngle);
            _transformController.RotationAngle += rotateAngle;
            return rotateAngle;
        }

        void ILocomotionController.StopRotate()
        {
            TokenCancel();
        }

        void ILocomotionController.SetSpeed(Vector3 direction, float? speed)
        {
            speed = speed != null ? speed : _speed;
            InternalSetSpeed(direction, (float)speed);
        }

        void ILocomotionController.Stop()
        {
            InternalStop();
        }

        void ILocomotionController.SnapHexCenter()
        {
            if (_transformController.GetLandedHex() == null) Debug.Log("null7");
            _transformController.Position = _transformController.GetLandedHex().transform.position;
        }

        protected virtual void Initialize()
        {

        }

        protected abstract void InternalSetSpeed(Vector3 direction, float speed);

        protected virtual void InternalStop()
        {
            Rigidbody.velocity = Vector3.zero;
        }

        void TokenCancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        void OnDestroy()
        {
            TokenCancel();
        }
    }
}
