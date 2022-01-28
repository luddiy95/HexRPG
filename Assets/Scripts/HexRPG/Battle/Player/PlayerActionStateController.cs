using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Battle.Stage;
    using UI;
    using static ActionStateType;

    public class PlayerActionStateController : AbstractCustomComponentBehaviour
    {
        public override void Initialize()
        {
            base.Initialize();
            BuildActionStates();
            SetUpControl();
        }

        void BuildActionStates()
        {
            if (Owner.QueryInterface(out IActionStateController actionStateController) == false)
            {
                Debug.LogError($"{Owner}が IActionStateController を持っていない");
                return;
            }

            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("pause", 0, PAUSE))
                .AddEvent(new ActionEventCancel("move", 0, MOVE))
                .AddEvent(new ActionEventCancel("damaged", 0, DAMAGED))
                ;
            actionStateController.SetInitialState(idle);

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

            ActionState NewState(ActionStateType type, System.Action<ActionState> action = null)
            {
                var s = new ActionState(type);
                actionStateController.AddState(s);
                action?.Invoke(s);
                return s;
            }
        }

        void SetUpControl()
        {
            Owner.QueryInterface(out ICharacterInput characterInput);

            Owner.QueryInterface(out IActionStateController actionStateController);
            Owner.QueryInterface(out IActionStateObservable actionStateObservable);

            Owner.QueryInterface(out ISkillController skillController);
            Owner.QueryInterface(out ISkillObservable skillObservable);

            Owner.QueryInterface(out IPauseController pauseController);
            Owner.QueryInterface(out IPauseObservable pauseObservable);

            Owner.QueryInterface(out ISelectSkillObservable selectSkillObservable);

            Owner.QueryInterface(out IAnimatorController animatorController);

            Owner.QueryInterface(out ITransformController transformController);

            // moveableIndicatorタップ時
            characterInput.Destination
                .Skip(1)
                .Subscribe(_ => 
                {
                    actionStateController.Execute(new Command { Id = "move" });
                })
                .AddTo(this);

            // moveに遷移出来たら移動
            if (Owner.QueryInterface(out IMoveController moveController))
            {
                actionStateObservable
                    .OnStart<ActionEventMove>()
                    .Subscribe(_ =>
                    {
                        moveController.StartMove(characterInput.Destination.Value);
                    })
                    .AddTo(this);
            }

            // btnFireタップ時
            characterInput.OnFire
            .Subscribe(_ =>
            {
                int skillIndex = selectSkillObservable.SelectedSkillIndex.Value;
                if (skillIndex >= 0)
                {
                    // Skillスタートできるか？
                    skillController.TryStartSkill(skillIndex, null);
                }
                else
                {
                    actionStateController.Execute(new Command { Id = "pause" });
                }
            })
            .AddTo(this);

            // Pauseへ遷移時
            actionStateObservable
                .OnStart<ActionEventPause>()
                .Subscribe(_ => pauseController.StartPause())
                .AddTo(this);

            // Skillスタート
            skillObservable
                .OnStartSkill
                .Subscribe(_ => actionStateController.ExecuteTransition(SKILL))
                .AddTo(this);

            // Restart
            pauseObservable.OnRestart
                .Subscribe(_ => actionStateController.ExecuteTransition(IDLE))
                .AddTo(this);

            // Skill終了
            skillObservable
                .OnFinishSkill
                .Subscribe(_ => actionStateController.ExecuteTransition(IDLE))
                .AddTo(this);

            // 各モーション再生
            actionStateObservable
                .OnStart<ActionEventPlayMotion>()
                .Subscribe(ev =>
                {
                    switch (actionStateObservable.CurrentState.Value.Type)
                    {
                        case IDLE:
                            break;

                        case MOVE:
                            Hex destinationHex = characterInput.Destination.Value;
                            Vector3 relativePos = destinationHex.transform.position - transformController.GetLandedHex().transform.position;
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

                            animatorController.SetSpeed(speedHorizontal, speedVertical);

                            break;

                        case DAMAGED:
                            break;
                    }
                })
                .AddTo(this);
        }
    }
}
