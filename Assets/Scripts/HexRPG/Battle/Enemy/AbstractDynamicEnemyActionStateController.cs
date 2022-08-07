using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Profiling;

namespace HexRPG.Battle.Enemy
{
    using Stage;
    using Stage.Tower;
    using static ActionStateType;

    public enum TargetType
    {
        PLAYER,
        PLAYER_TOWER
    }

    public abstract class AbstractDynamicEnemyActionStateController : MonoBehaviour, ICharacterActionStateController
    {
        IBattleObservable _battleObservable;
        IStageController _stageController;

        ITransformController _transformController;
        IAnimationController _animationController;

        INavMeshAgentController _navMeshAgentController;

        protected ILocomotionObservable _locomotionObservable;
        protected ILocomotionController _locomotionController;

        protected IActionStateController _actionStateController;
        protected IActionStateObservable _actionStateObservable;

        IDamageApplicable _damagedApplicable;
        IDieObservable _dieObservable;

        protected ICombatController _combatController;
        protected ICombatObservable _combatObservable;

        protected ISkillController _skillController;
        protected ISkillObservable _skillObservable;

        protected int _rotateAngle = 0;

        Vector3 _moveDirection;

        Hex _approachHex = null;
        Hex _attackableHex = null;

        Collider[] _colliders = new Collider[32];
        List<Hex> _surroundedHexList = new List<Hex>(256);
        List<Hex> _arroundHexList = new List<Hex>(128);
        List<Hex> _enemyHexList = new List<Hex>(512);

        protected CompositeDisposable _disposables = new CompositeDisposable();

        CancellationTokenSource _actionCts = null;
        CancellationTokenSource _moveCts = null;

        [Header("�^�[�Q�b�g")]
        [SerializeField] TargetType _targetType;

        [Header("��]")]
        [SerializeField] float MOVE_ROTATE_TIME = 0.1f;
        [SerializeField] protected float LOOK_ROTATE_SPEED = 240f;

        [Header("�U���͈�")]
        [SerializeField] float ATTACK_RADIUS = 5f;

        [Header("�C���^�[�o��(ms)")]
        [SerializeField] int IDLE_INTERVAL = 100;
        [SerializeField] int MOVE_SEARCH_INTERVAL = 1200; //(ms)

        float _stepDistanceByFrame;
        float _stepDistance2ByFrame;
        Vector3 _steeringTargetPosCache;

        ActionStateType CurState => _actionStateObservable.CurrentState.Value.Type;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            IStageController stageController,
            ITransformController transformController,
            IAnimationController animationController,
            INavMeshAgentController navMeshAgentController,
            ILocomotionController locomotionController,
            ILocomotionObservable locomotionObservable,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IDamageApplicable damageApplicable,
            IDieObservable dieObservable,
            ICombatController combatController,
            ICombatObservable combatObservable,
            ISkillController skillController,
            ISkillObservable skillObservable
        )
        {
            _battleObservable = battleObservable;
            _stageController = stageController;
            _transformController = transformController;
            _animationController = animationController;
            _navMeshAgentController = navMeshAgentController;
            _locomotionController = locomotionController;
            _locomotionObservable = locomotionObservable;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _damagedApplicable = damageApplicable;
            _dieObservable = dieObservable;
            _combatController = combatController;
            _combatObservable = combatObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
        }

        void ICharacterActionStateController.Init()
        {
            if (_actionStateObservable.CurrentState.Value == null)
            {
                _stepDistanceByFrame = _locomotionObservable.StepDistanceByFrame;
                _stepDistance2ByFrame = Mathf.Pow(_stepDistanceByFrame, 2);

                var initialState = BuildActionStates();
                SetUpControl();
                SetupState();
                _actionStateController.SetInitialState(initialState);
            }

            _navMeshAgentController.AgentEnable = true;

            _locomotionController.ForceLookRotate(_battleObservable.PlayerLandedHex.transform.position);
            SetDestinationLandedHex();

            _actionCts = new CancellationTokenSource();
            StartActionSequence(_actionCts.Token).Forget();
        }

        protected virtual ActionState BuildActionStates()
        {
            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("move", MOVE, passEndNotification: true)) // �����]��
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
                ;

            NewState(DIE)
                ;

            return idle;
        }

        protected ActionState NewState(ActionStateType type, Action<ActionState> action = null)
        {
            var s = new ActionState(type);
            _actionStateController.AddState(s);
            action?.Invoke(s);
            return s;
        }

        protected virtual void SetUpControl()
        {
            // NavMesh�̏󋵂��ς�����瑦�o�H�T��
            _battleObservable.OnCompleteUpdateNavMesh
                .Where(_ => _moveCts != null)
                .Subscribe(_ => SearchDestinationAfterUpdateNavMesh(_moveCts.Token).Forget())
                .AddTo(_disposables);

            // Damaged
            _damagedApplicable.OnHit
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if (hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        AllTokenCancel(); // Sequence���f

                        var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(_transformController.GetLandedHex().transform.position);
                        if (relativeDirFromHexCenter.sqrMagnitude < _stepDistance2ByFrame * 4)
                        {
                            SetDestinationLandedHex();
                        }
                        else
                        {
                            DeleteDestination();
                        }

                        _actionStateController.Execute(new Command { Id = "damaged" });
                    }
                })
                .AddTo(_disposables);

            _animationController.OnFinishDamaged
                .Subscribe(_ =>
                {
                    _actionCts = new CancellationTokenSource();
                    StartActionSequenceAfterDamaged(_actionCts.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    AllTokenCancel(); // Sequence���f

                    DeleteDestination();
                    _navMeshAgentController.AgentEnable = false;

                    _actionStateController.ExecuteTransition(DIE);
                })
                .AddTo(_disposables);
        }

        protected virtual void SetupState()
        {
            ////// �X�e�[�g�ł̏ڍ׏��� //////

            // �ړ��J�n/�I��
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ =>
                {
                    if (_moveDirection.sqrMagnitude > _stepDistance2ByFrame)
                        _locomotionController.FixTimeLookRotate(_transformController.Position + _moveDirection, MOVE_ROTATE_TIME);
                }).AddTo(_disposables);
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ =>
                {
                    _locomotionController.StopRotate();
                    _locomotionController.Stop();
                }).AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    _locomotionController.StopRotate();
                })
                .AddTo(_disposables);

            // �e���[�V�����Đ�
            _actionStateObservable
                .OnStart<ActionEventPlayMotion>()
                .Subscribe(ev =>
                {
                    switch (_actionStateObservable.CurrentState.Value.Type)
                    {
                        case IDLE:
                            _animationController.Play("Idle");
                            break;

                        case ROTATE:
                            if (_rotateAngle < 0) _animationController.Play("RotateLeft");
                            else _animationController.Play("RotateRight");
                            break;

                        case MOVE:
                            _animationController.Play("Movefwd");
                            break;

                        case DAMAGED:
                            _animationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        protected Hex TargetHex
        {
            get
            {
                var targetHex = _battleObservable.PlayerLandedHex;
                if (_targetType == TargetType.PLAYER_TOWER)
                {
                    var nearestPlayerTower = _battleObservable.TowerList
                        .Where(tower => tower.TowerObservable.TowerType.Value == TowerType.PLAYER)
                        .OrderBy(tower => MathUtility.GetDistance2XZ(_transformController.GetLandedHex(), tower.TowerObservable.TowerCenter))
                        .FirstOrDefault();
                    if (nearestPlayerTower != null) targetHex = nearestPlayerTower.TowerObservable.TowerCenter;
                }
                return targetHex;
            }
        }

        protected abstract ActionStateType AttackableBreakStateType { get; }

        async UniTaskVoid StartActionSequence(CancellationToken token)
        {
            _actionStateController.ExecuteTransition(IDLE);

            while (true)
            {
                // Idle
                await UniTask.Delay(IDLE_INTERVAL, cancellationToken: token);

                // Move
                _attackableHex = null;
                _approachHex = null;

                _moveCts = new CancellationTokenSource();
                var breakStateType = await StartMoveSequence(_moveCts.Token);
                MoveTokenCancel();

                await BreakState(breakStateType, token);
            }
        }

        protected virtual async UniTask BreakState(ActionStateType breakStateType, CancellationToken token)
        {
        }

        protected virtual async UniTask<ActionStateType> StartMoveSequence(CancellationToken token)
        {
            StartSearchDestinationIterater(token).Forget(); // ��莞�Ԃ��ƂɌo�H�T��

            ActionStateType breakActionStateType = NONE;

            CompositeDisposable disposables = new CompositeDisposable();
            token.Register(() => disposables.Dispose()); // token�L�����Z�����ɂ�fixedUpdate���~�߂�悤��

            // �����Player��Liberate�����Ȃ�A����Hex�t�߂ɂ���ꍇ�͐Î~����
            _battleObservable.OnReduceEnemyNavMesh
                .Where(_ => _navMeshAgentController.IsStopped == false)
                .Subscribe(liberateHexList =>
                {
                    if (liberateHexList.Contains(_transformController.GetLandedHex())) return; // liberateHexList��ɂ�����_���[�W�H�炤�̂Ŗ���

                    Array.Clear(_colliders, 0, _colliders.Length);
                    Physics.OverlapBoxNonAlloc(
                        _transformController.Position + _moveDirection.normalized * 0.25f,
                        new Vector3(0.125f, 0.125f, 0.25f),
                        _colliders,
                        Quaternion.LookRotation(_moveDirection),
                        TransformExtensions.hexLayerMask
                    );
                    var hitHexList = _colliders
                        .Select(collider => collider?.GetComponent<Hex>())
                        .Where(hex => hex != null);
                    if (hitHexList.Any(hex => liberateHexList.Contains(hex)))
                    {
                        // ActionEventMove��OnEnd����^�C�~���O�܂ő҂��Ă���stop����̂ł͒x��
                        _locomotionController.Stop();

                        DeleteDestination();
                        breakActionStateType = IDLE;
                    }
                })
                .AddTo(disposables);

            // FixedUpdate
            this.FixedUpdateAsObservable()
                .Subscribe(_ =>
                {
                    //! �ړ�
                    _navMeshAgentController.NextPosition = _transformController.Position;
                    if (_navMeshAgentController.IsStopped == false)
                    {
                        var steeringTargetPos = _navMeshAgentController.CurSteeringTargetPos;
                        if (MathUtility.GetDistance2XZ(steeringTargetPos, _steeringTargetPosCache) > _stepDistance2ByFrame)
                        {
                            _steeringTargetPosCache = steeringTargetPos;
                            _moveDirection = _transformController.Position.GetRelativePosXZ(steeringTargetPos);

                            _actionStateController.Execute(new Command { Id = "move" });
                            _locomotionController.SetSpeed(_moveDirection);
                        }
                    }

                    //! ���������H
                    var landedHex = _transformController.GetLandedHex();
                    var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

                    var enemyDestinationHexList = new List<Hex>(_battleObservable.EnemyDestinationHexList);
                    var curDestination = _navMeshAgentController.CurDestination.Value;
                    if (curDestination != null) enemyDestinationHexList.Remove(curDestination);

                    // Target���U���͈͓�
                    if (_attackableHex == landedHex)
                    {
                        if (relativeDirFromHexCenter.sqrMagnitude < _stepDistance2ByFrame * 4)
                        {
                            SetDestinationLandedHex();
                            breakActionStateType = AttackableBreakStateType; //! Target���U���͈͓��ł���hex�̒��S�ɒ�����
                        }

                        if (enemyDestinationHexList.Contains(landedHex))
                        {
                            DeleteDestination();
                            breakActionStateType = IDLE; //! �󋵂��ς��ړI�n�ɕʂ�Enemy���������Ă���
                        }
                    }

                    // ���݂�Hex���瓮���悤���Ȃ�
                    if (_approachHex == landedHex)
                    {
                        // Hex�̒��S�ɂ��邩�ǂ���
                        if (relativeDirFromHexCenter.sqrMagnitude < _stepDistance2ByFrame * 4)
                        {
                            SetDestinationLandedHex();
                            breakActionStateType = IDLE; //! ���݂�LandedHex���瓮���悤���Ȃ��Ă���hex�̒��S�ɂ���
                        }

                        if (enemyDestinationHexList.Contains(landedHex))
                        {
                            DeleteDestination();
                            breakActionStateType = IDLE; //! �󋵂��ς��ړI�n�ɕʂ�Enemy���������Ă���
                        }
                    }
                })
                .AddTo(disposables);

            await UniTask.WaitWhile(() => breakActionStateType == NONE, cancellationToken: token);

            disposables.Dispose();
            return breakActionStateType;
        }

        async UniTaskVoid StartSearchDestinationIterater(CancellationToken token)
        {
            while (true)
            {
                await UniTask.Delay(MOVE_SEARCH_INTERVAL, cancellationToken: token);
                SearchDestination();
            }
        }

        void SearchDestination()
        {
            var landedHex = _transformController.GetLandedHex();
            var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

            var targetHex = TargetHex;
            var distance2FromTargetHex = landedHex.GetDistance2XZ(targetHex);

            var enemyDestinationHexList = new List<Hex>(_battleObservable.EnemyDestinationHexList);
            var curDestination = _navMeshAgentController.CurDestination.Value;
            if (curDestination != null) enemyDestinationHexList.Remove(curDestination);

            if (_attackableHex == landedHex || _approachHex == landedHex) return; //! �ړI�n��hex�ɂ�����󋵂��ς���Ă�Destination��ύX���Ȃ�

            // target����ATTACK_RADIUS�̋����ȓ���Hex��Enemy���ړ��o����Hex�ōł�target�ɋ߂�Hex�ֈړ�
            TransformExtensions.GetSurroundedHexList(targetHex, ATTACK_RADIUS, ref _surroundedHexList);
            _enemyHexList.Clear();
            if (enemyDestinationHexList.Contains(landedHex) == false && _surroundedHexList.Contains(landedHex)) _enemyHexList.Add(landedHex);
            var surroundedEnemyHexList = _surroundedHexList
                .Where(hex =>
                        hex != targetHex
                        //&& hex.GetDistance2XZ(targetHex) + _stepDistanceByFrame < distance2FromTargetHex //! ���݂�LandedHex���TargetHex�ɋ߂�Hex(�~�܂��Ă����Ԃ���S������������hex�֐i�܂Ȃ��悤��)
                        && enemyDestinationHexList.Contains(hex) == false
                        && _navMeshAgentController.IsExistPath(hex.transform.position));
            _enemyHexList.AddRange(surroundedEnemyHexList);

            var attackableHex = _enemyHexList.OrderBy(hex => hex.GetDistance2XZ(targetHex)).FirstOrDefault();

            if (attackableHex != null)
            {
                _approachHex = null;

                //! ����_attackableHex������ALiberate�ɂ��󋵂��ς���Ă��炸(_attackableHex�֕ς�炸�ړ��\)�A���݂�_attackableHex ~ targetHex�Ԃ̋������Z���Ȃ�Ȃ��ꍇ�͍X�V���Ȃ�
                if (_attackableHex != null && _enemyHexList.Contains(_attackableHex)
                    && attackableHex.GetDistance2XZ(targetHex) + _stepDistance2ByFrame >= _attackableHex.GetDistance2XZ(targetHex))
                {
                    return; // ����_attackableHex��SetDestination����Ă���͂�
                }

                _attackableHex = attackableHex;
                SetDestination(_attackableHex);
                return; //! target�ɍU����������Hex�ֈړ�
            }

            _enemyHexList.Clear();
            if (enemyDestinationHexList.Contains(landedHex) == false) _enemyHexList.Add(landedHex);
            int radius = 1;
            while (true)
            {
                _stageController.GetArroundHexList(landedHex, radius, ref _arroundHexList);
                var aroundEnemyHexList = _arroundHexList
                    .Where(hex =>
                        hex != targetHex
                        //&& hex.GetDistance2XZ(targetHex) + _stepDistanceByFrame < distance2FromTargetHex //! ���݂�LandedHex���targetHex�ɋ߂�Hex(�~�܂��Ă����Ԃ���S������������hex�֐i�܂Ȃ��悤��)
                        && enemyDestinationHexList.Contains(hex) == false
                        && _navMeshAgentController.IsExistPath(hex.transform.position));

                _enemyHexList.AddRange(aroundEnemyHexList);

                if (_arroundHexList.Contains(targetHex)) break;
                ++radius;
            }

            var approachHex = _enemyHexList.OrderBy(hex => hex.GetDistance2XZ(targetHex)).FirstOrDefault();

            // ���t���[���̕]���ł�����IDLE��return�����
            if (approachHex == null) // landedHex�ɗ��܂邱�Ƃ�������Ȃ�
            {
                _approachHex = landedHex;
                return;
            }
            if (approachHex == landedHex && CurState == IDLE)
            {
                if (relativeDirFromHexCenter.sqrMagnitude < _stepDistance2ByFrame * 4)
                {
                    _approachHex = landedHex;
                    return; // hex�̐^�񒆂ŐÎ~���Ă����ԂŌ��݂�landedHex�œ����悤���Ȃ��ꍇ��MOVE�ɑJ�ڂ����Ȃ�
                }
            }

            //! ����_approachHex������A���݂�_approachHex ~ targetHex�Ԃ̋������Z���Ȃ�Ȃ��ꍇ�͍X�V���Ȃ�
            if (_approachHex != null && _enemyHexList.Contains(_approachHex)
                && approachHex.GetDistance2XZ(targetHex) + _stepDistance2ByFrame >= _approachHex.GetDistance2XZ(targetHex))
            {
                //SetDestination(_approachHex); // ����_approachHex��SetDestination����Ă���͂��Ȃ̂ł���Ȃ�
                return; // ���݂�_approachHex�̂܂ܑ��s
            }

            _approachHex = approachHex;
            SetDestination(_approachHex);
            return; //! �Ȃ�ׂ�target�ɋߕt�����Ƃ���
        }

        async UniTaskVoid SearchDestinationAfterUpdateNavMesh(CancellationToken token)
        {
            await UniTask.Yield(token); // NavMeshSurface��NavMesh���X�V����̂�҂�
            SearchDestination();
        }

        void SetDestination(Hex destination)
        {
            _navMeshAgentController.SetDestination(destination);
            _navMeshAgentController.IsStopped = false;
        }

        void SetDestinationLandedHex()
        {
            _navMeshAgentController.IsStopped = true;
            _navMeshAgentController.SetDestination(_transformController.GetLandedHex());
        }

        void DeleteDestination()
        {
            _navMeshAgentController.IsStopped = true;
            _navMeshAgentController.SetDestination(null);
        }

        async UniTaskVoid StartActionSequenceAfterDamaged(CancellationToken token)
        {
            _actionStateController.Execute(new Command { Id = "finishDamaged" });

            await UniTask.DelayFrame(1, cancellationToken: token);

            _actionStateController.Execute(new Command { Id = "rotate" });
            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
            _actionStateController.Execute(new Command { Id = "idle" });

            StartActionSequence(token).Forget();
        }

        void MoveTokenCancel()
        {
            _moveCts?.Cancel();
            _moveCts?.Dispose();
            _moveCts = null;
        }

        void AllTokenCancel()
        {
            MoveTokenCancel();

            _actionCts?.Cancel();
            _actionCts?.Dispose();
            _actionCts = null;
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            AllTokenCancel();
        }
    }
}
