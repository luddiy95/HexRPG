using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public class EnemyLocomotionBehaviour : LocomotionBehaviour
    {
        [Inject]
        public void Construct(
            ITransformController transformController
        )
        {
            _transformController = transformController;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetSpeed(Vector3 direction, float speed)
        {
            base.SetSpeed(direction, speed);
        }
    }
}
