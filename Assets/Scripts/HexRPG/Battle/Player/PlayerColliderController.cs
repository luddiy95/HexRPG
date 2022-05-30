using UnityEngine;
using System;
using Zenject;
using UniRx;

namespace HexRPG.Battle.Player
{
    public class PlayerColliderController : IColliderController, IInitializable, IDisposable
    {
        IMemberObservable _memberObservable;

        CapsuleCollider IColliderController.Collider => _collider;
        CapsuleCollider _collider;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IMemberObservable memberObservable
        )
        {
            _memberObservable = memberObservable;
        }

        void IInitializable.Initialize()
        {
            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(memberOwner => _collider = memberOwner.ColliderController.Collider)
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
