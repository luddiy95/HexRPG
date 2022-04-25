using UnityEngine;

namespace HexRPG.Battle
{
    public interface ILocomotionController
    {
        void SetSpeed(Vector3 direction, float? speed = null);
        void Stop();

        void SnapHexCenter();
    }

    public class LocomotionBehaviour : MonoBehaviour, ILocomotionController
    {
        protected ITransformController _transformController;

        protected Rigidbody Rigidbody => _rigidbody != null ? _rigidbody : GetComponent<Rigidbody>();
        [Header("������Rigidbody�Bnull�Ȃ炱�̃I�u�W�F�N�g")]
        [SerializeField] Rigidbody _rigidbody;

        //TODO: speed��setting����Ƃ��Ă���(Player�̏ꍇ��Member���ƂɈقȂ�)
        protected float _speed = 5f;
        protected float _colliderRadius = 0.5f;

        void Start()
        {
            Initialize();
        }

        void ILocomotionController.SetSpeed(Vector3 direction, float? speed)
        {
            speed = speed != null ? speed : _speed;
            SetSpeed(direction, (float)speed);
        }

        void ILocomotionController.Stop()
        {
            Rigidbody.velocity = Vector3.zero;
        }

        void ILocomotionController.SnapHexCenter()
        {
            _transformController.Position = _transformController.GetLandedHex().transform.position;
        }

        protected virtual void Initialize()
        {

        }

        protected virtual void SetSpeed(Vector3 direction, float speed)
        {

        }
    }
}
