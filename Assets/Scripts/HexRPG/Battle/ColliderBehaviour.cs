using UnityEngine;

namespace HexRPG.Battle
{
    public interface IColliderController
    {
        CapsuleCollider Collider { get; }
    }

    public class ColliderBehaviour : MonoBehaviour, IColliderController
    {
        CapsuleCollider IColliderController.Collider => _collider != null ? _collider : GetComponent<CapsuleCollider>();
        [Header("null ならこのオブジェクト。")]
        [SerializeField] CapsuleCollider _collider;
    }
}
