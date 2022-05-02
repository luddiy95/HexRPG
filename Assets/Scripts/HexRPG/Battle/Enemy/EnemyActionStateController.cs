using UnityEngine;
using Zenject;
using UniRx;
using System;

namespace HexRPG.Battle.Enemy
{
    using static ActionStateType;

    public class EnemyActionStateController : IInitializable, ITickable, IDisposable
    {
        IAnimationController _animationController;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IDieObservable _dieObservable;
        
        ISkillController _skillController;
        ISkillObservable _skillObservable;

        ActionStateType CurState => _actionStateObservable.CurrentState.Value.Type;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyActionStateController(
            IAnimationController animationController,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IDieObservable dieObservable,
            ISkillController skillController,
            ISkillObservable skillObservable
        )
        {
            _animationController = animationController;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _dieObservable = dieObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
        }

        void IInitializable.Initialize()
        {
            BuildActionStates();
            SetUpControl();
        }

        void ITickable.Tick()
        {
            //TODO: テストコード
            if (Input.GetKeyDown(KeyCode.A))
            {
                _actionStateController.Execute(new Command { Id = "skill" });
            }
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
                .AddEvent(new ActionEventCancel("finishSkill", IDLE))
                // IDLEに戻る
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
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ => _actionStateController.ExecuteTransition(DIE))
                .AddTo(_disposables);

            // Skill終了
            _skillObservable.OnFinishSkill
                .Where(_ => CurState == SKILL)
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "finishSkill" }))
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // スキル実行
            _actionStateObservable
                .OnStart<ActionEventSkill>()
                .Subscribe(_ =>
                {
                    _skillController.StartSkill(0); //TODO: 0は仮(複数Skill持つEnemyがいても良い)
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
                            _animationController.Play("Idle");
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
