using UnityEngine;
using System;
using Zenject;
using UniRx;

namespace HexRPG.Battle.HUD
{
    public class DamagedPanelParentHUD : MonoBehaviour, ICharacterHUD, IPoolable<IMemoryPool>, IDisposable
    {
        IMemoryPool _pool;

        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            for (int i = 0; i < transform.childCount; i++)
            {
                var huds = transform.GetChild(i).GetComponents<ICharacterHUD>();
                foreach (var hud in huds) hud.Bind(character);
            }
        }

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

        public class Factory : PlaceholderFactory<DamagedPanelParentHUD>
        {

        }

        public class Pool : MonoPoolableMemoryPool<IMemoryPool, DamagedPanelParentHUD>
        {

        }
    }
}
