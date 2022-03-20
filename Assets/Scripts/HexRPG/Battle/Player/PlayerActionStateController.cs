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

        ICharacterInput _characterInput;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IMemberObservable _memberObservable;

        ICombatController _combatController;
        ICombatObservable _combatObservable;

        ISelectSkillObservable _selectSkillObservable;
        ISelectSkillController _selectSkillController;
        ISkillController _skillController;
        ISkillObservable _skillObservable;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerActionStateController(
            ILocomotionController locomotionController,
            IAnimatorController animatorController,
            ICharacterInput characterInput,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IMemberObservable memberObservable,
            ICombatController combatController,
            ICombatObservable combatObservable,
            ISkillController skillController,
            ISkillObservable skillObservable,
            ISelectSkillObservable selectSkillObservable,
            ISelectSkillController selectSkillController
        )
        {
            _locomotionController = locomotionController;
            _animatorController = animatorController;
            _characterInput = characterInput;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _memberObservable = memberObservable;
            _combatController = combatController;
            _combatObservable = combatObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
            _selectSkillObservable = selectSkillObservable;
            _selectSkillController = selectSkillController;
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
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", SKILL_SELECT))
                ;
            _actionStateController.SetInitialState(idle);

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("move", MOVE, passEndNotification: true)) // 方向変更
                .AddEvent(new ActionEventCancel("stop", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", SKILL_SELECT))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                ;

            //TODO: エフェクトのことを考えるとTimelineにするべき？
            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT, passEndNotification: true)) //TODO: ？(Combat中でもcombat入力を受け取り連続コンボなどに利用できる)
                // IDLEに戻る
                // MOVEに戻る
                ;

            NewState(SKILL_SELECT)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventSkillSelect())
                .AddEvent(new ActionEventCancel("skillCancel", IDLE))
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", SKILL_SELECT, passEndNotification: true))
                .AddEvent(new ActionEventCancel("skill", SKILL, passEndNotification: true))
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
            ////// Player入力などによる状態遷移 //////
            
            // joyスティック入力時
            _characterInput.Direction
                .Subscribe(direction =>
                {
                    if(direction.magnitude > 0.1) _actionStateController.Execute(new Command { Id = "move" });
                    else _actionStateController.Execute(new Command { Id = "stop" });
                })
                .AddTo(_disposables);

            // Combat
            _characterInput.OnCombat
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "combat" });
                })
                .AddTo(_disposables);

            // Combat終了
            _combatObservable.OnFinishCombat
                .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                .AddTo(_disposables);

            // Skill選択
            _characterInput.SelectedSkillIndex
                .Skip(1)
                .Subscribe(index =>
                {
                    if (index > _memberObservable.CurMember.Value.SkillSpawnObservable.SkillList.Length - 1) return;
                    //TODO: MPやチャージ状態を見て選択可能か判断(UIにも反映)、実行できないSkillはそもそも選択できないようにする
                    _actionStateController.Execute(new Command { Id = "skillSelect" });
                })
                .AddTo(_disposables);

            // Skillキャンセル時
            _characterInput.OnSkillCancel
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "skillCancel" }))
                .AddTo(_disposables);
            _characterInput.CameraRotateDir
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "skillCancel" }))
                .AddTo(_disposables);

            // Skill決定時
            _characterInput.OnSkillDecide
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "skill" });
                })
                .AddTo(_disposables);

            // Skill終了
            _skillObservable
                .OnFinishSkill
                .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // 移動開始/終了
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ => _locomotionController.SetSpeed(_characterInput.Direction.Value))
                .AddTo(_disposables);
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ => {
                    _locomotionController.Stop();
                    _animatorController.SetSpeed(0, 0); //TODO: Playableに変えたら消す(?)、animation関係は全てActionEventPlayMotionに集約させる
                }).AddTo(_disposables);

            // Combat
            _actionStateObservable
                .OnStart<ActionEventCombat>()
                .Subscribe(_ =>
                {
                    _combatController.Combat();
                })
                .AddTo(_disposables);

            // スキル選択時
            _actionStateObservable
                .OnStart<ActionEventSkillSelect>()
                .Subscribe(_ => _selectSkillController.SelectSkill(_characterInput.SelectedSkillIndex.Value))
                .AddTo(_disposables);

            // スキル選択解除
            _actionStateObservable
                .OnEnd<ActionEventSkillSelect>()
                .Subscribe(_ => _selectSkillController.ResetSelection())
                .AddTo(_disposables);

            // スキル実行
            _actionStateObservable
                .OnStart<ActionEventSkill>()
                .Subscribe(_ =>
                {
                    _skillController.StartSkill(_selectSkillObservable.SelectedSkillIndex.Value, null);
                    _selectSkillController.ResetSelection();
                })
                .AddTo(_disposables);

            // Hexの中心にSnap
            _actionStateObservable
                .OnStart<ActionEventSnapHexCenter>()
                .Subscribe(_ => _locomotionController.SnapHexCenter())
                .AddTo(_disposables);

            // 各モーション再生
            //TODO: Playableにする
            _actionStateObservable
                .OnStart<ActionEventPlayMotion>()
                .Subscribe(ev =>
                {
                    switch (_actionStateObservable.CurrentState.Value.Type)
                    {
                        case IDLE:
                            break;

                        case MOVE:
                            var direction = _characterInput.Direction.Value;
                            _animatorController.SetSpeed(direction.x, direction.z);
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
