using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Battle.Stage;
    using static ActionStateType;

    public class PlayerActionStateController : IInitializable, IDisposable
    {
        ITransformController _transformController;
        IAnimatorController _animatorController;
        ICharacterInput _characterInput;
        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;
        ISkillController _skillController;
        ISkillObservable _skillObservable;
        IPauseController _pauseController;
        IPauseObservable _pauseObservable;
        ISelectSkillObservable _selectSkillObservable;
        IMoveController _moveController;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerActionStateController(
            ITransformController transformController,
            IAnimatorController animatorController,
            ICharacterInput characterInput,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            ISkillController skillController,
            ISkillObservable skillObservable,
            IPauseController pauseController,
            IPauseObservable pauseObservable,
            ISelectSkillObservable selectSkillObservable,
            IMoveController moveController
        )
        {
            _transformController = transformController;
            _animatorController = animatorController;
            _characterInput = characterInput;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
            _pauseController = pauseController;
            _pauseObservable = pauseObservable;
            _selectSkillObservable = selectSkillObservable;
            _moveController = moveController;
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
                .AddEvent(new ActionEventCancel("pause", 0, PAUSE))
                .AddEvent(new ActionEventCancel("move", 0, MOVE))
                .AddEvent(new ActionEventCancel("damaged", 0, DAMAGED))
                ;
            _actionStateController.SetInitialState(idle);

            NewState(PAUSE)
                .AddEvent(new ActionEventPause(0f)) // Pause中
                // Skillへ遷移
                // IDLEに戻る
                ;

            NewState(MOVE)
                .AddEvent(new ActionEventMove(0f)) // 移動中
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("damaged", 0, DAMAGED))
                // IDLEに戻る
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                ;

            NewState(SKILL)
                .AddEvent(new ActionEventSkill(0f))
                // IDLEに戻る
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
            // moveableIndicatorタップ時
            _characterInput.Destination
                .Skip(1)
                .Subscribe(_ => 
                {
                    _actionStateController.Execute(new Command { Id = "move" });
                })
                .AddTo(_disposables);

            // moveに遷移出来たら移動
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ =>
                {
                    _moveController.StartMove(_characterInput.Destination.Value);
                })
                .AddTo(_disposables);

            // btnFireタップ時
            _characterInput.OnFire
            .Subscribe(_ =>
            {
                int skillIndex = _selectSkillObservable.SelectedSkillIndex.Value;
                if (skillIndex >= 0)
                {
                    // Skillスタートできるか？->できたらそのまま実行
                    _skillController.TryStartSkill(skillIndex);
                }
                else
                {
                    _actionStateController.Execute(new Command { Id = "pause" });
                }
            })
            .AddTo(_disposables);

            // Pauseへ遷移時
            _actionStateObservable
                .OnStart<ActionEventPause>()
                .Subscribe(_ => _pauseController.StartPause())
                .AddTo(_disposables);

            //! Skillスタート
            _skillObservable
                .OnStartSkill
                .Subscribe(_ => _actionStateController.ExecuteTransition(SKILL))
                .AddTo(_disposables);

            // Restart
            _pauseObservable.OnRestart
                .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                .AddTo(_disposables);

            //! Skill終了
            _skillObservable
                .OnFinishSkill
                .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                .AddTo(_disposables);

            // 各モーション再生
            _actionStateObservable
                .OnStart<ActionEventPlayMotion>()
                .Subscribe(ev =>
                {
                    switch (_actionStateObservable.CurrentState.Value.Type)
                    {
                        case IDLE:
                            break;

                        case MOVE:
                            Hex destinationHex = _characterInput.Destination.Value;
                            Vector3 relativePos = destinationHex.transform.position - _transformController.GetLandedHex().transform.position;
                            relativePos.y = 0;
                            Quaternion relativeRot = Quaternion.LookRotation(relativePos, Vector3.up);
                            int relativeRotAngle = (int)relativeRot.eulerAngles.y;
                            float speedHorizontal = 0f, speedVertical = 0f;
                            if (0 < relativeRotAngle && relativeRotAngle < 60)
                            {
                                speedVertical = 1f;
                            }
                            else if (60 < relativeRotAngle && relativeRotAngle < 120)
                            {
                                speedVertical = 1f; speedHorizontal = 1f;
                            }
                            else if (120 < relativeRotAngle && relativeRotAngle < 180)
                            {
                                speedVertical = -1f; speedHorizontal = 1f;
                            }
                            else if (180 < relativeRotAngle && relativeRotAngle < 240)
                            {
                                speedVertical = -1f;
                            }
                            else if (240 < relativeRotAngle && relativeRotAngle < 300)
                            {
                                speedVertical = -1f; speedHorizontal = -1f;
                            }
                            else if (300 < relativeRotAngle && relativeRotAngle < 360)
                            {
                                speedVertical = 1f; speedHorizontal = -1f;
                            }

                            _animatorController.SetSpeed(speedHorizontal, speedVertical);

                            break;

                        case DAMAGED:
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
