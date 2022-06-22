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

        ISkillController _skillController;
        ISkillObservable _skillObservable;

        int _rotateAngle = 0;

        readonly IReactiveProperty<Vector3> _moveDirection = new ReactiveProperty<Vector3>();

        CompositeDisposable _disposables = new CompositeDisposable();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        const float COMBAT_DISTANCE = 5f;

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
            ICombatObservable combatObservable,
            ISkillController skillController,
            ISkillObservable skillObservable
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
            _skillController = skillController;
            _skillObservable = skillObservable;
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
                .AddEvent(new ActionEventCancel("move", MOVE))
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
                // IDLE�ɖ߂�
                ;

            NewState(SKILL)
                .AddEvent(new ActionEventSkill())
                .AddEvent(new ActionEventCancel("finishSkill", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                // IDLE�ɖ߂�
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
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ => _navMeshAgentController.NextPosition = _transformController.Position)
                .AddTo(_disposables);

            // Move
            _moveDirection
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "move" }))
                .AddTo(_disposables);

            // Damaged
            _damagedApplicable.OnHit
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if (hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        _cancellationTokenSource.Cancel(); // Sequence���f
                        _actionStateController.Execute(new Command { Id = "damaged" });
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
                    if(_moveDirection.Value.sqrMagnitude > 0.1) _locomotionController.ForceLookRotate(_transformController.Position + _moveDirection.Value);
                    _locomotionController.SetSpeed(_moveDirection.Value);
                })
                .AddTo(_disposables);
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ => {
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

            // �X�L�����s
            _actionStateObservable
                .OnStart<ActionEventSkill>()
                .Subscribe(_ =>
                {
                    _skillController.StartSkill(0); //TODO: 0�͉�(����Skill����Enemy�����Ă��ǂ�)
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
                await UniTask.Delay(2000, cancellationToken: token);

                ActionStateType breakStateType = NONE;

                //TODO: UpdateTiming�������H(200ms�����Ƃ�)
                await UniTask.WaitUntil(() =>
                {
                    var landedHex = _transformController.GetLandedHex();
                    var playerLandedHex = _battleObservable.PlayerLandedHex;
                    var distance2FromPlayerHex = landedHex.GetDistance2XZ(playerLandedHex); // ������Player��LandedHex���m�̋���

                    // Player���U���͈͓�
                    if (Mathf.Sqrt(distance2FromPlayerHex) <= COMBAT_DISTANCE)
                    {
                        var relativeDir = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);
                        if (relativeDir.sqrMagnitude < 0.025f)
                        {
                            breakStateType = COMBAT;
                            return true; //! player���U���͈͓��ł���hex�̒��S�ɒ�����
                        }

                        _moveDirection.Value = relativeDir;
                        return false; //! player���U���͈͓�����hex�̒��S�łȂ�
                    }

                    // Destination��path��Liberate�ɂ���ď󋵂��ς����
                    var directionHex = TransformExtensions.GetLandedHex(_transformController.Position + _moveDirection.Value.normalized * 0.1f);
                    if (directionHex.IsPlayerHex)
                    {
                        SetDestination(landedHex);
                        return false;
                    }

                    // Player����COMBAT_DISTANCE�̋����ȓ���Hex��Enemy���ړ��o����Hex�ōł��߂�Hex�ֈړ�
                    Hex nearestHex = TransformExtensions.GetSurroundedHexList(playerLandedHex, COMBAT_DISTANCE)
                        .Where(hex => hex.IsPlayerHex == false && _navMeshAgentController.IsExistPath(hex.transform.position))
                        .OrderBy(hex => hex.GetDistance2XZ(landedHex))
                        .FirstOrDefault();

                    if (nearestHex != null)
                    {
                        SetDestination(nearestHex);
                        return false; //! Player�ɍU����������Hex�ֈړ�
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

                    var farestHex = enemyHexList.OrderBy(hex => hex.GetDistance2XZ(playerLandedHex)).First();

                    // ���݂�Hex���瓮���悤���Ȃ�
                    if (farestHex == landedHex)
                    {
                        // Hex�̒��S�ɂ��邩�ǂ���
                        var relativeDir = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);
                        if (relativeDir.sqrMagnitude < 0.025f)
                        {
                            breakStateType = IDLE;
                            return true; //! ���݂�LandedHex���瓮���悤���Ȃ��Ă���hex�̒��S�ɂ���
                        }

                        _moveDirection.Value = relativeDir;
                        return false; //! ���݂�LandedHex���瓮���悤���Ȃ��Ă���hex�̒��S�łȂ�
                    }

                    SetDestination(farestHex);
                    return false; //! �Ȃ�ׂ�Player�ɋߕt�����Ƃ���
                });

                switch (breakStateType)
                {
                    case IDLE:
                        if(CurState == MOVE)
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
                        _actionStateController.Execute(new Command { Id = "finishCombat" }); break;
                }

                // Skill
                /*
                _actionStateController.Execute(new Command { Id = "skill" });
                await _skillObservable.OnFinishSkill.ToUniTask(useFirstValue: true, cancellationToken: token);
                _actionStateController.Execute(new Command { Id = "finishSkill" });
                */
            }
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
