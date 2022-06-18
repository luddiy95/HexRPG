using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public interface INavMeshAgentController
    {
        bool SetDestination(Vector3 targetPos);

        bool IsExistPath { get; }
        Vector3 CurSteeringTarget { get; }

        bool IsPathComplete { get; }

        void ResetPath();
    }

    public class EnemyLocomotionBehaviour : AbstractLocomotionBehaviour, INavMeshAgentController
    {
        NavMeshAgent NavMeshAgent => _navMeshAgent ? _navMeshAgent : GetComponent<NavMeshAgent>();
        [Header("NavMeshAgent。nullならこのオブジェクト")]
        [SerializeField] NavMeshAgent _navMeshAgent;

        bool INavMeshAgentController.IsExistPath => NavMeshAgent.path != null;
        bool INavMeshAgentController.IsPathComplete => NavMeshAgent.pathStatus == NavMeshPathStatus.PathComplete;

        Vector3 INavMeshAgentController.CurSteeringTarget => NavMeshAgent.steeringTarget;

        [Inject]
        public void Construct(
            ITransformController transformController
        )
        {
            _transformController = transformController;
        }

        protected override void Initialize()
        {
            NavMeshAgent.updateRotation = false;
            base.Initialize();
        }

        protected override void SetSpeed(Vector3 direction, float speed)
        {
            base.SetSpeed(direction, speed);
        }

        bool INavMeshAgentController.SetDestination(Vector3 targetPos)
        {
            return NavMeshAgent.SetDestination(targetPos);
        }

        void INavMeshAgentController.ResetPath()
        {
            NavMeshAgent.ResetPath();
        }
    }
}
