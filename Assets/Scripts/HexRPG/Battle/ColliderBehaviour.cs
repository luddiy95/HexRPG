using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public interface IColliderController
    {
        CapsuleCollider Collider { get; }
    }

    public class ColliderBehaviour : MonoBehaviour, IColliderController
    {
        IDieObservable _dieObservable;

        CapsuleCollider IColliderController.Collider => _collider ? _collider! : _collider = GetComponent<CapsuleCollider>();

#nullable enable
        [Header("null ならこのオブジェクト。")]
        [SerializeField] CapsuleCollider? _collider;
#nullable disable

        [Inject]
        public void Construct(
            IDieObservable dieObservable
        )
        {
            _dieObservable = dieObservable;
        }

        void Start()
        {
            _dieObservable.IsDead
                .Subscribe(isDead => (this as IColliderController).Collider.enabled = !isDead)
                .AddTo(this);
        }
    }
}
