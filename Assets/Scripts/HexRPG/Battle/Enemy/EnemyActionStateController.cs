using Zenject;
using UniRx;
using System;
using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    using Player;
    using static ActionStateType;

    public class EnemyActionStateController : IInitializable, IDisposable
    {
        IBattleObservable _battleObservable;
        IPauseObservable _pauseObservable;
        ITransformController _transformController;
        IAnimatorController _animatorController;
        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;
        IDamageApplicable _damageApplicable;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyActionStateController(
            IBattleObservable battleObservable,
            IPauseObservable pauseObservable,
            ITransformController transformController,
            IAnimatorController animatorController,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IDamageApplicable damageApplicable
        )
        {
            _battleObservable = battleObservable;
            _pauseObservable = pauseObservable;
            _transformController = transformController;
            _animatorController = animatorController;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _damageApplicable = damageApplicable;
        }

        void IInitializable.Initialize()
        {
            BuildActionStates();
            SetUpControl();
        }

        void BuildActionStates()
        {
            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("move", 0, MOVE))
                .AddEvent(new ActionEventCancel("skill", 0, SKILL))
                ;
            _actionStateController.SetInitialState(idle);

            NewState(MOVE)
                .AddEvent(new ActionEventMove(0f)) // �ړ���
                .AddEvent(new ActionEventPlayMotion(0f))
                // IDLE�ɖ߂�
                ;

            NewState(PAUSE) //! Player�̍U���͈͓��ɂ���Ƃ��̂�PlayerSkill��������Pause��ԂɑJ��
                .AddEvent(new ActionEventPause(0f)) // Pause��
                .AddEvent(new ActionEventPlayMotion(0f)) // idle���[�V����
                .AddEvent(new ActionEventCancel("damaged", 0, DAMAGED)) // �_���[�W���󂯂�
                // IDLE�ɖ߂�
                ;
            ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                // PAUSE�ɖ߂�
                ;

            NewState(SKILL)
                .AddEvent(new ActionEventSkill())
                // IDLE�ɖ߂�
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
            ////// Execute(Player�ɂ��Command) //////
            // Pause
            _pauseObservable.OnPause
                .Subscribe(_ => _animatorController.Pause()) //! �����ł�Animator��Pause���邾��
                .AddTo(_disposables);

            // Restart
            _pauseObservable.OnRestart
                .Subscribe(_ => {
                    _animatorController.Restart();
                    if(_actionStateObservable.CurrentState.Value.Type == DAMAGED)
                    {
                        //! DAMAGED��Ԃ����� <==> Player�̍U���͈͓��ɂ���(�_���[�W��H�����)
                        _actionStateController.ExecuteTransition(IDLE);
                    }
                    else
                    {
                        //! Player�ɍU������Ȃ������ꍇ��IDLE��ԂɑJ�ڂ������O�̏�Ԃ𑱍s
                    }
                })
                .AddTo(_disposables);

            // Player��Skill�X�^�[�g����Skill�͈͓���Enemy��������
            _battleObservable.OnPlayerSpawn
                .Subscribe(playerOwner =>
                {
                    playerOwner.SkillObservable.OnStartSkill
                        .Subscribe(_ =>
                        {
                            var isInPlayerAttackRange = false;
                            foreach (var attackReservation in _transformController.GetLandedHex().AttackReservationList)
                            {
                                if (attackReservation.ReservationOrigin is IPlayerComponentCollection)
                                {
                                    isInPlayerAttackRange = true;
                                    break;
                                }
                            }
                            if (isInPlayerAttackRange)
                            {
                                _actionStateController.ExecuteTransition(PAUSE);
                            }
                        }).AddTo(_disposables);
                }).AddTo(_disposables);

            // �_���[�W��
            _damageApplicable.OnHit
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "damaged" }))
                .AddTo(_disposables);

            ////// ActionStateObservable //////
            _actionStateObservable
                .OnStart<ActionEventPause>()
                .Subscribe(ev =>
                {
                    _animatorController.Restart();
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
                            _animatorController.SetTrigger("Idle");
                            break;

                        case PAUSE:
                            _animatorController.SetTrigger("Pause", "Idle");
                            break;

                        case MOVE:
                            break;

                        case DAMAGED:
                            _animatorController.SetTrigger("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
