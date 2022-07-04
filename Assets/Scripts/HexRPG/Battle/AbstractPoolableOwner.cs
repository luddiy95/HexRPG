using UnityEngine;
using System;
using Zenject;

namespace HexRPG.Battle
{
    public class AbstractPoolableOwner<T> : MonoBehaviour, IPoolable<Transform, Vector3, IMemoryPool>, IDisposable
        where T : MonoBehaviour, IPoolable<Transform, Vector3, IMemoryPool>
    {
        [Inject] public ITransformController TransformController { get; }

        IMemoryPool _pool;

        void IPoolable<Transform, Vector3, IMemoryPool>.OnSpawned(Transform spawnRoot, Vector3 spawnPos, IMemoryPool pool)
        {
            _pool = pool;
            TransformController.Init(spawnRoot, spawnPos);
        }

        void IPoolable<Transform, Vector3, IMemoryPool>.OnDespawned()
        {
            _pool = null;
        }

        public void Dispose()
        {
            _pool.Despawn(this);
        }

        public class Factory : PlaceholderFactory<Transform, Vector3, T>
        {

        }

        public class Pool : MonoPoolableMemoryPool<Transform, Vector3, IMemoryPool, T>
        {

        }
    }
}
