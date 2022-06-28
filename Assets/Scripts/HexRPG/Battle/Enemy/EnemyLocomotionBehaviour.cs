using UnityEngine;
using UnityEngine.AI;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public interface INavMeshAgentController
    {
        bool IsExistPath(Vector3 destination);

        bool SetDestination(Vector3 targetPos);
        Vector3 CurSteeringTargetPos { get; }
        Vector3 NextPosition { set; }

        bool IsStopped { get; set; }
    }

    public class EnemyLocomotionBehaviour : AbstractLocomotionBehaviour, INavMeshAgentController
    {
        IMoveSetting _moveSetting;

        NavMeshAgent NavMeshAgent => _navMeshAgent ? _navMeshAgent : GetComponent<NavMeshAgent>();
        [Header("NavMeshAgent。nullならこのオブジェクト")]
        [SerializeField] NavMeshAgent _navMeshAgent;

        Vector3 INavMeshAgentController.CurSteeringTargetPos => NavMeshAgent.steeringTarget;

        Vector3 INavMeshAgentController.NextPosition { set { NavMeshAgent.nextPosition = value; } }

        bool INavMeshAgentController.IsStopped
        {
            get => NavMeshAgent.isStopped;
            set
            {
                if (value) NavMeshAgent.ResetPath();
                NavMeshAgent.isStopped = value;
            }
        }

        [Inject]
        public void Construct(
            IMoveSetting moveSetting,
            ITransformController transformController
        )
        {
            _moveSetting = moveSetting;
            _transformController = transformController;
        }

        protected override void Initialize()
        {
            _speed = _moveSetting.MoveSpeed;

            NavMeshAgent.updateRotation = false;
            NavMeshAgent.updatePosition = false;

            base.Initialize();
        }

        protected override void InternalSetSpeed(Vector3 direction, float speed)
        {
            Rigidbody.velocity = direction.normalized * speed;
        }

        bool INavMeshAgentController.IsExistPath(Vector3 destination)
        {
            var path = new NavMeshPath();
            NavMeshAgent.CalculatePath(destination, path);
            return path.status == NavMeshPathStatus.PathComplete;
        }

        bool INavMeshAgentController.SetDestination(Vector3 targetPos)
        {
            return NavMeshAgent.SetDestination(targetPos);
        }
    }
}
