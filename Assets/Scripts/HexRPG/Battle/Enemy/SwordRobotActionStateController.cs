using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    using Stage;
    using static ActionStateType;

    public class SwordRobotActionStateController : MonoBehaviour, ICharacterActionStateController
    {
        IUpdateObservable _updateObservable;
        IBattleObservable _battleObservable;
        IStageController _stageController;

        ITransformController _transformController;
        IAnimationController _animationController;

        INavMeshAgentController _navMeshAgentController;

        ILocomotionObservable _locomotionObservable;
        ILocomotionController _locomotionController;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IDamageApplicable _damagedApplicable;
        IDieObservable _dieObservable;

        ICombatController _combatController;
        ICombatObservable _combatObservable;

        int _rotateAngle = 0;

        readonly IReactiveProperty<Vector3> _moveDirection = new ReactiveProperty<Vector3>();

        Hex _approachHex = null;
        Hex _attackableHex = null;

        CompositeDisposable _disposables = new CompositeDisposable();
        CancellationTokenSource _actionCancellationTokenSource = new CancellationTokenSource();
        CancellationTokenSource _moveCancellationTokenSource = new CancellationTokenSource();

        const float COMBAT_DISTANCE = 5f;
        const int MOVE_SEARCH_INTERVAL = 1200; //(ms)

        ActionStateType CurState => _actionStateObservable.CurrentState.Value.Type;

        [Inject]
        public void Construct(
            IUpdateObservable updateObservable,
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
            ICombatObservable combatObservable
        )
        {
            _updateObservable = updateObservable;
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
        }

        void ICharacterActionStateController.Init()
        {
            BuildActionStates();
            SetUpControl();

            _locomotionController.ForceLookRotate(_battleObservable.PlayerLandedHex.transform.position);
            StartActionSequence(_actionCancellationTokenSource.Token).Forget();
        }

        void Update()
        {
            //TODO: �e�X�g�R�[�h
            if (Input.GetKeyDown(KeyCode.A))
            {
                _actionStateController.Execute(new Command { Id = "combat" });
            }
        }

        void BuildActionStates()
        {
            // �uRotate, Damaged, Die��ExecuteTransaction�őJ�ځv

            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;
            _actionStateController.SetInitialState(idle);

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("move", MOVE, passEndNotification: true))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;

            NewState(ROTATE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
                ;

            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("finishCombat", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(DIE)
                ;

            ActionState NewState(ActionStateType type, Action<ActionState> action = null)
            {
                var s = new ActionState(type);
                _actionStateController.AddState(s);
                action?.Invoke(s);
                return s;
            }
        }

        void SetUpControl()
        {
            // Move
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _navMeshAgentController.NextPosition = _transformController.Position;
                    if (!_navMeshAgentController.IsStopped)
                    {
                        // �o�H�T���O��Delay�����ړ�
                        _moveDirection.Value = _transformController.Position.GetRelativePosXZ(_navMeshAgentController.CurSteeringTargetPos);
                    }
                })
                .AddTo(_disposables);

            _moveDirection
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "move" });
                })
                .AddTo(_disposables);

            // Damaged
            _damagedApplicable.OnHit
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if (hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        AllTokenCancel(); // Sequence���f

                        _navMeshAgentController.IsStopped = true;
                        _actionStateController.Execute(new Command { Id = "damaged" });
                    }
                })
                .AddTo(_disposables);

            _animationController.OnFinishDamaged
                .Subscribe(_ =>
                {
                    _actionCancellationTokenSource = new CancellationTokenSource();
                    StartActionSequenceAfterDamaged(_actionCancellationTokenSource.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    AllTokenCancel(); // Sequence���f

                    _navMeshAgentController.IsStopped = true;
                    _actionStateController.ExecuteTransition(DIE);
                    OnDestroy();
                })
                .AddTo(_disposables);

            ////// �X�e�[�g�ł̏ڍ׏��� //////

            // �ړ��J�n/�I��
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ =>
                {
                    if (_moveDirection.Value.sqrMagnitude > 0.1) _locomotionController.ForceLookRotate(_transformController.Position + _moveDirection.Value);
                    _locomotionController.SetSpeed(_moveDirection.Value);
                })
                .AddTo(_disposables);
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ =>
                {
                    _locomotionController.Stop();
                }).AddTo(_disposables);

            // Rotate
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    _rotateAngle = _locomotionController.LookRotate(_battleObservable.OnPlayerSpawn.Value.TransformController.Position, 240f);
                })
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    _locomotionController.StopRotate();
                })
                .AddTo(_disposables);

            // Combat
            _actionStateObservable
                .OnStart<ActionEventCombat>()
                .Subscribe(_ =>
                {
                    _combatController.Combat();
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

        async UniTaskVoid StartActionSequence(CancellationToken token)
        {
            while (true)
            {
                // Idle
                await UniTask.Delay(100, cancellationToken: token);

                // Move
                _attackableHex = null;
                _approachHex = null;

                _moveCancellationTokenSource = new CancellationTokenSource();
                var breakStateType = await StartMoveSequence(_moveCancellationTokenSource.Token);
                _moveCancellationTokenSource.Cancel();

                switch (breakStateType)
                {
                    case IDLE:
                        if (CurState == MOVE) // �����Ă�����Ԃ��瓮���悤���Ȃ���ԂɂȂ����Ƃ��̍ŏ��̈��(Move -> Idle)�̂�Player�̕�������
                        {
                            _actionStateController.Execute(new Command { Id = "rotate" });
                            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                            _actionStateController.Execute(new Command { Id = "idle" });
                        }
                        break;
                    case COMBAT:
                        _actionStateController.Execute(new Command { Id = "rotate" });
                        await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                        // Combat
                        _actionStateController.Execute(new Command { Id = "combat" });
                        await _combatObservable.OnFinishCombat.ToUniTask(useFirstValue: true, cancellationToken: token);
                        _actionStateController.Execute(new Command { Id = "finishCombat" });

                        await UniTask.Delay(1000, cancellationToken: token); // Delay��������������idle->rotate�J�ڒ���idle���荞�݂̉\��������

                        _actionStateController.Execute(new Command { Id = "rotate" });
                        await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                        _actionStateController.Execute(new Command { Id = "idle" }); break;
                }
            }
        }

        async UniTask<ActionStateType> StartMoveSequence(CancellationToken token)
        {
            SearchDestination(token).Forget(); // 200ms���ƂɌo�H�T��

            while (true)
            {
                var landedHex = _transformController.GetLandedHex();
                var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

                // Destination��path��Liberate�ɂ���ď󋵂��ς����
                var directionHex = TransformExtensions.GetLandedHex(_transformController.Position + _moveDirection.Value.normalized * 0.1f);
                if (directionHex.IsPlayerHex)
                {
                    _navMeshAgentController.SetDestination(landedHex.transform.position);
                    continue; //! Destination��path���hex��playerHex�ɕω������̂Ői�H�ύX
                }

                // Player���U���͈͓�
                if (_attackableHex == landedHex)
                {
                    if (relativeDirFromHexCenter.sqrMagnitude < 0.1f) //? 0.1f > �umoveSpeed( /s) * 0.016(s)�v
                    {
                        _navMeshAgentController.IsStopped = true;
                        return COMBAT; //! player���U���͈͓��ł���hex�̒��S�ɒ�����
                    }
                }

                // ���݂�Hex���瓮���悤���Ȃ�
                if (_approachHex == landedHex)
                {
                    // Hex�̒��S�ɂ��邩�ǂ���
                    if (relativeDirFromHexCenter.sqrMagnitude < 0.1f)
                    {
                        _navMeshAgentController.IsStopped = true;
                        return IDLE; //! ���݂�LandedHex���瓮���悤���Ȃ��Ă���hex�̒��S�ɂ���
                    }
                }

                await UniTask.WaitForEndOfFrame(cancellationToken: token);
                continue;
            };
        }

        async UniTaskVoid SearchDestination(CancellationToken token)
        {
            while (true)
            {
                await UniTask.Delay(MOVE_SEARCH_INTERVAL, cancellationToken: token);

                var landedHex = _transformController.GetLandedHex();
                var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

                var playerLandedHex = _battleObservable.PlayerLandedHex;
                var distance2FromPlayerHex = landedHex.GetDistance2XZ(playerLandedHex);

                if (_attackableHex == landedHex || _approachHex == landedHex) continue; //! �ړI�n��hex�ɂ�����󋵂��ς���Ă�Destination��ύX���Ȃ�

                // Player����COMBAT_DISTANCE�̋����ȓ���Hex��Enemy���ړ��o����Hex�ōł��߂�Hex�ֈړ�
                _attackableHex = TransformExtensions.GetSurroundedHexList(playerLandedHex, COMBAT_DISTANCE)
                    .Where(hex => hex.IsPlayerHex == false && _navMeshAgentController.IsExistPath(hex.transform.position))
                    .OrderBy(hex => hex.GetDistance2XZ(landedHex))
                    .FirstOrDefault();

                if (_attackableHex != null)
                {
                    _navMeshAgentController.SetDestination(_attackableHex.transform.position);
                    _navMeshAgentController.IsStopped = false;
                    continue; //! Player�ɍU����������Hex�ֈړ�
                }

                int radius = 0;
                var enemyHexList = new List<Hex>();
                while (true)
                {
                    var aroundHexList = _stageController.GetAroundHexList(landedHex, radius);
                    var aroundEnemyHexList = aroundHexList.Where(hex =>
                        hex.IsPlayerHex == false
                        && _navMeshAgentController.IsExistPath(hex.transform.position)
                        && hex.GetDistance2XZ(playerLandedHex) <= distance2FromPlayerHex); // ���݂�PlayerLandedHex�ւ̋������Z���Ȃ�
                    enemyHexList.AddRange(aroundEnemyHexList);
                    if (aroundHexList.Any(hex => hex == playerLandedHex)) break;
                    ++radius;
                }

                _approachHex = enemyHexList.OrderBy(hex => hex.GetDistance2XZ(playerLandedHex)).First();

                _navMeshAgentController.SetDestination(_approachHex.transform.position);
                if(_approachHex == landedHex && CurState == IDLE)
                {
                    if (relativeDirFromHexCenter.sqrMagnitude < 0.1f)
                    {
                        continue; // hex�̐^�񒆂ŐÎ~���Ă����ԂŌ��݂�landedHex�œ����悤���Ȃ��ꍇ��MOVE�ɑJ�ڂ����Ȃ�
                    }
                }
                
                _navMeshAgentController.IsStopped = false;
                continue; //! �Ȃ�ׂ�Player�ɋߕt�����Ƃ���
            }
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

        void AllTokenCancel()
        {
            _moveCancellationTokenSource.Cancel();
            _actionCancellationTokenSource.Cancel();
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            _moveCancellationTokenSource.Dispose();
            _actionCancellationTokenSource.Dispose();
        }
    }
}
