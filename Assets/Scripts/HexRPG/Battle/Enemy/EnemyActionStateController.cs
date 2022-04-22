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
        IAnimatorController _animatorController;
        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyActionStateController(
            IAnimatorController animatorController,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable
        )
        {
            _animatorController = animatorController;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
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
                .AddEvent(new ActionEventMove(0f)) // 移動中
                .AddEvent(new ActionEventPlayMotion(0f))
                // IDLEに戻る
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                ;

            NewState(SKILL)
                .AddEvent(new ActionEventSkill())
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

            ////// ActionStateObservable //////

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
