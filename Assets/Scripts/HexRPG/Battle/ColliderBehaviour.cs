using UnityEngine;

namespace HexRPG.Battle
{
    public interface IColliderController
    {
        CapsuleCollider Collider { get; }
    }

    public class ColliderBehaviour : MonoBehaviour, IColliderController
    {
        public CapsuleCollider Collider => _collider ? _collider : _collider = GetComponent<CapsuleCollider>();
        [Header("null �Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] CapsuleCollider _collider;
    }
}
