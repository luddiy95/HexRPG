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
                .Where(isDead => isDead)
                .Subscribe(_ => (this as IColliderController).Collider.enabled = false)
                .AddTo(this);
        }
    }
}
