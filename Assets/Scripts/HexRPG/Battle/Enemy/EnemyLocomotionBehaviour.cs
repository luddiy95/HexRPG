using UnityEngine;
using UnityEngine.AI;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    using Stage;

    public interface INavMeshAgentController
    {
        bool AgentEnable { set; }

        bool IsExistPath(Vector3 destination);

        void SetDestination(Hex destination);
        IReadOnlyReactiveProperty<Hex> CurDestination { get; }

        Vector3 CurSteeringTargetPos { get; }

        Vector3 NextPosition { set; }

        bool IsStopped { get; set; }
    }

    public class EnemyLocomotionBehaviour : AbstractLocomotionBehaviour, INavMeshAgentController
    {
        IMoveSetting _moveSetting;

        NavMeshAgent NavMeshAgent => _navMeshAgent ? _navMeshAgent : _navMeshAgent = GetComponent<NavMeshAgent>();
        [Header("NavMeshAgent。nullならこのオブジェクト")]
        [SerializeField] NavMeshAgent _navMeshAgent;

        IReadOnlyReactiveProperty<Hex> INavMeshAgentController.CurDestination => _curDestination;
        readonly IReactiveProperty<Hex> _curDestination = new ReactiveProperty<Hex>();

        Vector3 INavMeshAgentController.CurSteeringTargetPos => NavMeshAgent.steeringTarget;

        Vector3 INavMeshAgentController.NextPosition { set { NavMeshAgent.nextPosition = value; } }

        NavMeshPath _path;

        bool INavMeshAgentController.AgentEnable { set => NavMeshAgent.enabled = value; }

        bool INavMeshAgentController.IsStopped
        {
            get => _isStopped;
            set => _isStopped = value;
        }

        bool _isStopped = true;

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

            _path = new NavMeshPath();

            base.Initialize();
        }

        protected override void InternalSetSpeed(Vector3 direction, float speed)
        {
            Rigidbody.velocity = direction.normalized * speed;
        }

        bool INavMeshAgentController.IsExistPath(Vector3 destination)
        {
            return NavMeshAgent.CalculatePath(destination, _path) && _path.status == NavMeshPathStatus.PathComplete;
        }

        void INavMeshAgentController.SetDestination(Hex destination)
        {
            _curDestination.Value = destination;
            if (destination == null) NavMeshAgent.ResetPath();
            else NavMeshAgent.SetDestination(destination.transform.position);
        }
    }
}
