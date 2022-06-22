using System.Collections;
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

        CompositeDisposable _disposables = new CompositeDisposable();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        const float COMBAT_DISTANCE = 5f;
        const int MOVE_INTERVAL = 200; //(ms)

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
            StartSequence(_cancellationTokenSource.Token).Forget();
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
                ;
            _actionStateController.SetInitialState(idle);

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("move", MOVE))
                ;

            NewState(ROTATE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
                ;

            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("finishCombat", IDLE))
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
                    if (CurState == MOVE)
                    {
                        // move�C���^�[�o�������ړI�n�ֈړ�
                        _moveDirection.Value = _transformController.Position.GetRelativePosXZ(_navMeshAgentController.CurSteeringTargetPos);
                    }
                })
                .AddTo(_disposables);

            _moveDirection
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "move" }))
                .AddTo(_disposables);

            // Damaged
            _damagedApplicable.OnHit
                .Where(_ => CurState != DAMAGED) // Enemy��Damaged�X�e�[�g����Damaged�X�e�[�g�֑J�ڂ��Ȃ�
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if (hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        _cancellationTokenSource.Cancel(); // Sequence���f
                        _actionStateController.ExecuteTransition(DAMAGED);
                    }
                })
                .AddTo(_disposables);

            _animationController.OnFinishDamaged
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "finishDamaged" });

                    // Sequence�ĊJ
                    _cancellationTokenSource = new CancellationTokenSource();
                    StartSequence(_cancellationTokenSource.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    _cancellationTokenSource.Cancel(); // Sequence���f
                    _actionStateController.ExecuteTransition(DIE);
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
                            /*
                            var euler = Quaternion.LookRotation(_moveDirection.Value).eulerAngles.y - _transformController.Rotation.eulerAngles.y;
                            euler = MathUtility.GetIntegerEuler(euler) + 180;
                            var locomotionIndex = ((int)((euler + 22.5) / 45)) % 8;
                            _animationController.Play(AnimationExtensions.MoveClips[locomotionIndex]);
                            */
                            _animationController.Play("Movefwd");
                            break;

                        case DAMAGED:
                            _animationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        void SetDestination(Hex destinationHex)
        {
            _navMeshAgentController.SetDestination(destinationHex.transform.position);
            _moveDirection.Value = _transformController.Position.GetRelativePosXZ(_navMeshAgentController.CurSteeringTargetPos);
        }

        async UniTaskVoid StartSequence(CancellationToken token)
        {
            while (true)
            {
                // Idle
                await UniTask.Delay(1000, cancellationToken: token);

                Hex farestHex = null;
                ActionStateType breakStateType = NONE;

                // Move
                while (true)
                {
                    var landedHex = _transformController.GetLandedHex();
                    var relativeDir = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

                    var playerLandedHex = _battleObservable.PlayerLandedHex;
                    var distance2FromPlayerHex = landedHex.GetDistance2XZ(playerLandedHex); // ������Player��LandedHex���m�̋���

                    // Destination��path��Liberate�ɂ���ď󋵂��ς����
                    var directionHex = TransformExtensions.GetLandedHex(_transformController.Position + _moveDirection.Value.normalized * 0.1f);
                    if (directionHex.IsPlayerHex)
                    {
                        SetDestination(landedHex);
                        await UniTask.WaitForEndOfFrame();
                        continue; //! Destination��path���hex��playerHex�ɕω������̂Ői�H�ύX
                    }

                    if (relativeDir.sqrMagnitude < 1f) //? 1f > �umoveSpeed( /s) * MOVE_INTERVAL(s)�v
                    {
                        // Player���U���͈͓�
                        if (Mathf.Sqrt(distance2FromPlayerHex) <= COMBAT_DISTANCE)
                        {
                            if (relativeDir.sqrMagnitude < 0.1f) //? 0.1f > �umoveSpeed( /s) * 0.016(s)�v
                            {
                                breakStateType = COMBAT;
                                break; //! player���U���͈͓��ł���hex�̒��S�ɒ�����
                            }

                            await UniTask.WaitForEndOfFrame();
                            continue; //! player���U���͈͓�����hex�̒��S�łȂ�
                        }

                        // ���݂�Hex���瓮���悤���Ȃ�
                        if (farestHex == landedHex)
                        {
                            // Hex�̒��S�ɂ��邩�ǂ���
                            if (relativeDir.sqrMagnitude < 0.1f)
                            {
                                breakStateType = IDLE;
                                break; //! ���݂�LandedHex���瓮���悤���Ȃ��Ă���hex�̒��S�ɂ���
                            }

                            await UniTask.WaitForEndOfFrame();
                            continue; //! ���݂�LandedHex���瓮���悤���Ȃ��Ă���hex�̒��S�łȂ�
                        }
                    }

                    // Player����COMBAT_DISTANCE�̋����ȓ���Hex��Enemy���ړ��o����Hex�ōł��߂�Hex�ֈړ�
                    Hex nearestHex = TransformExtensions.GetSurroundedHexList(playerLandedHex, COMBAT_DISTANCE)
                        .Where(hex => hex.IsPlayerHex == false && _navMeshAgentController.IsExistPath(hex.transform.position))
                        .OrderBy(hex => hex.GetDistance2XZ(landedHex))
                        .FirstOrDefault();

                    if (nearestHex != null)
                    {
                        SetDestination(nearestHex);
                        await UniTask.Delay(MOVE_INTERVAL, cancellationToken: token);
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

                    farestHex = enemyHexList.OrderBy(hex => hex.GetDistance2XZ(playerLandedHex)).First();

                    SetDestination(farestHex);
                    await UniTask.Delay(MOVE_INTERVAL, cancellationToken: token);
                    continue; //! �Ȃ�ׂ�Player�ɋߕt�����Ƃ���
                };

                switch (breakStateType)
                {
                    case IDLE:
                        if (CurState == MOVE) // �����Ă�����Ԃ��瓮���悤���Ȃ���ԂɂȂ����Ƃ��̍ŏ��̈��(Move -> Idle)�̂�Player�̕�������
                        {
                            _actionStateController.ExecuteTransition(ROTATE); // _updateObservable.OnUpdate�ł�_moveDirection�ύX�ɂ��move�R�}���h�ɏ㏑������Ȃ��悤�ɂ���
                            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                            _actionStateController.Execute(new Command { Id = "idle" });
                        }
                        break;
                    case COMBAT:
                        _actionStateController.ExecuteTransition(ROTATE); // _updateObservable.OnUpdate�ł�_moveDirection�ύX�ɂ��move�R�}���h�ɏ㏑������Ȃ��悤�ɂ���
                        await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                        // Combat
                        _actionStateController.Execute(new Command { Id = "combat" });
                        await _combatObservable.OnFinishCombat.ToUniTask(useFirstValue: true, cancellationToken: token);
                        _actionStateController.Execute(new Command { Id = "finishCombat" }); break;
                }
            }
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
