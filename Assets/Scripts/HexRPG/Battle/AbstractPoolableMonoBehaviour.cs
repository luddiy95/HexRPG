using UnityEngine;
using System;
using Zenject;

namespace HexRPG.Battle
{
    public class AbstractPoolableMonoBehaviour<T> : MonoBehaviour, IPoolable<IMemoryPool>, IDisposable where T : MonoBehaviour, IPoolable<IMemoryPool>
    {
        IMemoryPool _pool;

        void IPoolable<IMemoryPool>.OnSpawned(IMemoryPool pool)
        {
            _pool = pool;
        }

        void IPoolable<IMemoryPool>.OnDespawned()
        {
            _pool = null;
        }

        public void Dispose()
        {
            _pool.Despawn(this);
        }

        public class Factory : PlaceholderFactory<T>
        {

        }

        public class Pool : MonoPoolableMemoryPool<IMemoryPool, T>
        {

        }
    }
}
