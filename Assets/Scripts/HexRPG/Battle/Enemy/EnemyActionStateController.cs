using Zenject;

namespace HexRPG.Battle.Enemy
{
    using static ActionStateType;

    public class EnemyActionStateController : IInitializable
    {
        IActionStateController _actionStateController;

        public EnemyActionStateController(IActionStateController actionStateController)
        {
            _actionStateController = actionStateController;
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
                .AddEvent(new ActionEventCancel("damaged", 0, DAMAGED))
                ;
            _actionStateController.SetInitialState(idle);
                ;

            NewState(MOVE)
                .AddEvent(new ActionEventMove(0f)) // 移動中
                .AddEvent(new ActionEventPlayMotion(0f))
                // IDLEに戻る
                ;

            NewState(PAUSE)
                .AddEvent(new ActionEventPause(0f)) // Pause中
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("damaged", 0, DAMAGED)) // ダメージを受ける
                // IDLEに戻る
                ;
            ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                // IDLEに戻る
                ;

            NewState(SKILL)
                .AddEvent(new ActionEventSkill(0f))
                // IDLEに戻る
                ;

            ActionState NewState(ActionStateType type, System.Action<ActionState> action = null)
            {
                var s = new ActionState(type);
                _actionStateController.AddState(s);
                action?.Invoke(s);
                return s;
            }
        }

        void SetUpControl()
        {
            /*
            Owner.QueryInterface(out ICharacterInput characterInput);

            Owner.QueryInterface(out IActionStateController actionStateController);
            Owner.QueryInterface(out IActionStateObservable actionStateObservable);

            Owner.QueryInterface(out IDamageApplicable damageApplicable);

            Owner.QueryInterface(out ISkillController skillController);
            Owner.QueryInterface(out ISkillObservable skillObservable);

            Owner.QueryInterface(out IAnimatorController animatorController);

            Owner.QueryInterface(out ITransformController transformController);

            damageApplicable.OnHit
                .Subscribe(_ => actionStateController.Execute(new Command { Id = "damaged" }));

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
                            //TODO: 目的の方向へ回転してから移動開始
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
                            animatorController.SetTrigger("Damaged");
                            break;
                    }
                })
                .AddTo(this);
            */
        }
    }
}
