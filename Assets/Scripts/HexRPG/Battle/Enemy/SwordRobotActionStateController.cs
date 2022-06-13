using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    using Stage;
    using static ActionStateType;

    public class SwordRobotActionStateController : MonoBehaviour, ICharacterActionStateController
    {
        IBattleObservable _battleObservable;
        IUpdateObservable _updateObservable;
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

        ISkillController _skillController;
        ISkillObservable _skillObservable;

        readonly IReactiveProperty<Vector3> _chaseDirection = new ReactiveProperty<Vector3>();

        CompositeDisposable _disposables = new CompositeDisposable();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        ActionStateType CurState => _actionStateObservable.CurrentState.Value.Type;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            IUpdateObservable updateObservable,
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
            ISkillController skillController,
            ISkillObservable skillObservable
        )
        {
            _battleObservable = battleObservable;
            _updateObservable = updateObservable;
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
                _actionStateController.Execute(new Command { Id = "skill" });
            }
        }

        void BuildActionStates()
        {
            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;
            _actionStateController.SetInitialState(idle);

            //TODO: CHASE�X�e�[�g�����K�v�H
            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("skill", SKILL))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(ROTATE)
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            //TODO: Damaged�I������Idle�ɖ߂�Ƃ��K��60�x�P�ʂɂ���
            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
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
            // Move
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Where(_ => _navMeshAgentController.IsExistPath)
                .Subscribe(_ =>
                {
                    var relativePos = _navMeshAgentController.CurSteeringTarget - _transformController.Position;
                    relativePos.y = 0;
                    _chaseDirection.Value = relativePos.normalized;
                })
                .AddTo(_disposables);

            _chaseDirection // �����]�����������Ƃ�
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

            // Rotate
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ => _locomotionController.LookRotate60(_battleObservable.PlayerLandedHex.transform.position, 240f))
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventRotate>()
                .Subscribe(_ => _locomotionController.StopRotate())
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

                        case MOVE:
                            var euler = Quaternion.LookRotation(_chaseDirection.Value).eulerAngles.y - _transformController.Rotation.eulerAngles.y;
                            euler = MathUtility.GetIntegerEuler(euler) + 180;
                            var locomotionIndex = ((int)((euler + 22.5) / 45)) % 8;
                            _animationController.Play(AnimationExtensions.MoveClips[locomotionIndex]);
                            break;

                        case DAMAGED:
                            _animationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        async UniTaskVoid StartSequence(CancellationToken token)
        {
            while (true)
            {
                // Idle
                await UniTask.Delay(2000, cancellationToken: token);

                // Player�̕��։�]
                _actionStateController.Execute(new Command { Id = "rotate" });
                await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                // Chase�ł����Player��Chase
                var curRotateAngle = MathUtility.GetIntegerEuler60(_transformController.Rotation.eulerAngles.y); // Player�̕��������Ă���
                var destinationHex = _stageController.GetNearestEnemyHexFromAngle(_battleObservable.PlayerLandedHex, curRotateAngle + 180);

                if (destinationHex != null && _navMeshAgentController.SetDestination(destinationHex.transform.position))
                {
                    Debug.Log("Chase");
                }
                else
                {
                    //TODO: �K���ɓ���
                }

                await UniTask.WaitWhile(() => _navMeshAgentController.IsPathComplete == false, cancellationToken: token);

                // Skill
                _actionStateController.Execute(new Command { Id = "skill" });
                await _skillObservable.OnFinishSkill.ToUniTask(useFirstValue: true, cancellationToken: token);
                _actionStateController.Execute(new Command { Id = "finishSkill" });
            }
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
