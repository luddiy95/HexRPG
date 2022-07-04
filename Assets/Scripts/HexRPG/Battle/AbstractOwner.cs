using UnityEngine;
using Zenject;

namespace HexRPG.Battle
{
    public abstract class AbstractOwner<T> : MonoBehaviour where T : IBaseComponentCollection
    {
        [Inject] public ITransformController TransformController { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, T>
        {
            public override T Create(Transform spawnRoot, Vector3 spawnPos)
            {
                var owner = base.Create(spawnRoot, spawnPos);
                owner.TransformController.Init(spawnRoot, spawnPos);
                return owner;
            }
        }
    }
}
