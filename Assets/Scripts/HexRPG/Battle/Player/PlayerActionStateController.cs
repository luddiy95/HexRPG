using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using static ActionStateType;

    public class PlayerActionStateController : IInitializable, IDisposable
    {
        ILocomotionController _locomotionController;
        IAnimatorController _animatorController;
        IUpdateObservable _updateObservable;
        ICharacterInput _characterInput;
        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;
        ISkillController _skillController;
        ISkillObservable _skillObservable;
        IPauseController _pauseController;
        IPauseObservable _pauseObservable;
        ISelectSkillObservable _selectSkillObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerActionStateController(
            ILocomotionController locomotionController,
            IAnimatorController animatorController,
            IUpdateObservable updateObservable,
            ICharacterInput characterInput,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            ISkillController skillController,
            ISkillObservable skillObservable,
            IPauseController pauseController,
            IPauseObservable pauseObservable,
            ISelectSkillObservable selectSkillObservable
        )
        {
            _locomotionController = locomotionController;
            _animatorController = animatorController;
            _updateObservable = updateObservable;
            _characterInput = characterInput;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
            _pauseController = pauseController;
            _pauseObservable = pauseObservable;
            _selectSkillObservable = selectSkillObservable;
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
                //TODO: ここでのActionEventPlayMotionを廃止し、InputのDirection変更を拾う->それに応じてvelocity/アニメーション変更するActionEventを作る
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("stop", 0, IDLE))
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
            ////// Execute(PlayerによるCommand) //////
            // moveableIndicatorタップ時
            /*
            _characterInput.Destination
                .Skip(1)
                .Subscribe(_ => 
                {
                    _actionStateController.Execute(new Command { Id = "move" });
                })
                .AddTo(_disposables);
            */

            // joyスティック入力時
            var canMove = false;
            _updateObservable.OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    var direction = _characterInput.Direction.Value;
                    if (direction.magnitude > 0.1)
                    {
                        if (canMove)
                        {
                            _locomotionController.SetSpeed(direction);

                            _animatorController.SetSpeed(direction.x, direction.z);
                        }
                        else
                        {
                            _actionStateController.Execute(new Command { Id = "move" });
                        }
                    }
                    else if (canMove)
                    {
                        _locomotionController.Stop();
                        _animatorController.SetSpeed(0, 0);
                        _actionStateController.Execute(new Command { Id = "stop" });
                    }
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

            //! Skillスタート
            _skillObservable
                .OnStartSkill
                .Subscribe(_ => _actionStateController.ExecuteTransition(SKILL))
                .AddTo(_disposables);

            // Restart
            _pauseObservable.OnRestart
                .Subscribe(_ => {
                    _actionStateController.ExecuteTransition(IDLE);
                    _animatorController.Restart();
                })
                .AddTo(_disposables);

            //! Skill終了
            _skillObservable
                .OnFinishSkill
                .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                .AddTo(_disposables);

            ////// ActionStateObservable //////
            // moveに遷移出来たら移動
            /*
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ =>
                {
                    _moveController.StartMove(_characterInput.Destination.Value);
                })
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ =>
                {

                })
                .AddTo(_disposables);
            */
            _actionStateObservable.OnStart<ActionEventMove>().Subscribe(_ => canMove = true).AddTo(_disposables);
            _actionStateObservable.OnEnd<ActionEventMove>().Subscribe(_ => canMove = false).AddTo(_disposables);

            // Pauseへ遷移時
            _actionStateObservable
                .OnStart<ActionEventPause>()
                .Subscribe(_ => {
                    _pauseController.StartPause();
                    _animatorController.Pause();
                })
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
