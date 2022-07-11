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

        public CapsuleCollider Collider => _collider ? _collider : _collider = GetComponent<CapsuleCollider>();
        [Header("null ならこのオブジェクト。")]
        [SerializeField] CapsuleCollider _collider;

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
                .Subscribe(isDead => Collider.enabled = !isDead)
                .AddTo(this);
        }
    }
}
