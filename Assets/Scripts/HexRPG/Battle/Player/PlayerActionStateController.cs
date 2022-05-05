using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using static ActionStateType;

    public class PlayerActionStateController : ICharacterActionStateController, IDisposable
    {
        ITransformController _transformController;

        ILocomotionController _locomotionController;
        ILocomotionObservable _locomotionObservable;

        ICharacterInput _characterInput;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IMemberObservable _memberObservable;

        IDamageApplicable _damagedApplicable;

        ICombatController _combatController;
        ICombatObservable _combatObservable;

        ISelectSkillObservable _selectSkillObservable;
        ISelectSkillController _selectSkillController;
        ISkillController _skillController;
        ISkillObservable _skillObservable;

        ActionStateType CurState => _actionStateObservable.CurrentState.Value.Type;
        ActionStateType PrevState => _actionStateObservable.PreviousState.Type;

        bool _acceptDirectionInput = true;

        int _rotateAngle = 0;
        float _rotateTime = 0.26f; //! durationより短いとダメ(予期せぬ遷移中割り込み)

        CompositeDisposable _disposables = new CompositeDisposable();
        CompositeDisposable _memberChangeDisposables = new CompositeDisposable();

        public PlayerActionStateController(
            ITransformController transformController,
            ILocomotionController locomotionController,
            ILocomotionObservable locomotionObservable,
            ICharacterInput characterInput,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IMemberObservable memberObservable,
            IDamageApplicable damageApplicable,
            ICombatController combatController,
            ICombatObservable combatObservable,
            ISkillController skillController,
            ISkillObservable skillObservable,
            ISelectSkillObservable selectSkillObservable,
            ISelectSkillController selectSkillController
        )
        {
            _transformController = transformController;
            _locomotionController = locomotionController;
            _locomotionObservable = locomotionObservable;
            _characterInput = characterInput;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _memberObservable = memberObservable;
            _damagedApplicable = damageApplicable;
            _combatController = combatController;
            _combatObservable = combatObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
            _selectSkillObservable = selectSkillObservable;
            _selectSkillController = selectSkillController;
        }

        void ICharacterActionStateController.Init()
        {
            BuildActionStates();

            _actionStateObservable.CurrentState
                .Where(state => state != null)
                .First() //! 初期化時CurMemberが設定されてからIDLEステートに遷移したときの最初の一回
                .Subscribe(_ =>
                {
                    SetUpControl();
                })
                .AddTo(_disposables);

            // Member変更
            _memberObservable.CurMember
                .Subscribe(member =>
                {
                    _actionStateController.ExecuteTransition(IDLE);

                    _memberChangeDisposables.Clear();
                    member.AnimationController.OnFinishDamaged
                        .Where(_ => CurState == DAMAGED)
                        .Subscribe(_ =>
                        {
                            if(_transformController.RotationAngle == 0)
                            {
                                _actionStateController.Execute(new Command { Id = "finishDamaged" });
                            }
                            else
                            {
                                _actionStateController.Execute(new Command { Id = "rotate" });
                            }
                        })
                        .AddTo(_memberChangeDisposables);

                    member.DieObservable.IsDead
                        .Where(isDead => isDead)
                        .Subscribe(_ => _actionStateController.ExecuteTransition(DIE))
                        .AddTo(_memberChangeDisposables);
                })
                .AddTo(_disposables);
        }

        void BuildActionStates()
        {
            NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventIdle(0f))
                .AddEvent(new ActionEventCancel("move", 0.35f, MOVE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", 0.35f, COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", 0.35f, SKILL_SELECT))
                ;

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("move", MOVE, passEndNotification: true)) // 方向変更
                .AddEvent(new ActionEventCancel("stop", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", SKILL_SELECT))
                ;

            NewState(ROTATE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("skill", SKILL))
                .AddEvent(new ActionEventCancel("idle", IDLE))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
                ;

            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT, passEndNotification: true))
                .AddEvent(new ActionEventCancel("finishCombat", IDLE))
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
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("finishSkill", IDLE))
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
            ////// Player入力などによる状態遷移 //////
            /// ※ UPDATE_ORDER.INPUTで同時にInputされた場合(例えばDirection, Combat)、↓のSubscribe記述順にCommandが実行されるため記述の順序に注意

            // Damaged
            _damagedApplicable.OnHit
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "damaged" }))
                .AddTo(_disposables);

            // joyスティック入力時
            _characterInput.Direction
                .Subscribe(direction =>
                {
                    if (direction.magnitude > 0.1)
                    {
                        var canMoveState = (CurState == IDLE || CurState == MOVE || CurState == SKILL_SELECT);
                        if(canMoveState && _acceptDirectionInput) _actionStateController.Execute(new Command { Id = "move" });
                    }
                    else
                    {
                        var isMoveState = (CurState == MOVE);
                        if(isMoveState) _actionStateController.Execute(new Command { Id = "stop" });

                        // Skill選択ステートに遷移した後、一度Direction入力がzeroにならないとDirection入力による移動を受け付けない
                        if (CurState == SKILL_SELECT) _acceptDirectionInput = true; 
                    }
                })
                .AddTo(_disposables);

            // Combat
            _characterInput.OnCombat
                .Where(_ => CurState == IDLE || CurState == MOVE || CurState == COMBAT || CurState == SKILL_SELECT)
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "combat" });
                })
                .AddTo(_disposables);

            // Combat終了
            _combatObservable.OnFinishCombat
                .Where(_ => CurState == COMBAT)
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "finishCombat" });
                })
                .AddTo(_disposables);

            // Skill選択
            _characterInput.SelectedSkillIndex
                .Where(_ => CurState == IDLE || CurState == MOVE || CurState == SKILL_SELECT)
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
                .Where(_ => CurState == SKILL_SELECT)
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "skillCancel" }))
                .AddTo(_disposables);
            _characterInput.CameraRotateDir
                .Where(_ => CurState == SKILL_SELECT)
                .Subscribe(_ => _actionStateController.Execute(new Command { Id = "skillCancel" }))
                .AddTo(_disposables);

            // Skill決定時
            _characterInput.OnSkillDecide
                .Where(_ => CurState == SKILL_SELECT)
                .Subscribe(_ =>
                {
                    if (_selectSkillObservable.SelectedSkillRotation != 0)
                    {
                        //! 回転しながらSkill実行
                        _locomotionController.FixTimeRotate(_selectSkillObservable.SelectedSkillRotation, _rotateTime);
                    }
                    _actionStateController.Execute(new Command { Id = "skill" });
                })
                .AddTo(_disposables);

            _locomotionObservable.OnFinishRotate
                .Where(_ => PrevState == DAMAGED || PrevState == SKILL_SELECT || PrevState == SKILL)
                .Subscribe(_ =>
                {
                    switch (PrevState)
                    {
                        case DAMAGED:
                        case SKILL:
                            _actionStateController.Execute(new Command { Id = "idle" });
                            break;
                    }
                })
                .AddTo(_disposables);

            // Skill終了
            _skillObservable.OnFinishSkill
                .Where(_ => CurState == SKILL)
                .Subscribe(_ =>
                {
                    if(_selectSkillObservable.SelectedSkillRotation == 0)
                    {
                        _actionStateController.Execute(new Command { Id = "finishSkill" });
                    }
                    else
                    {
                        _actionStateController.Execute(new Command { Id = "rotate" });
                    }
                })
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // Idle遷移時
            _actionStateObservable
                .OnStart<ActionEventIdle>()
                .Subscribe(_ =>
                {
                    //TODO:
                    // これを使って_characterInputを制御するフラグを立てたりすれば、IDLE遷移前に可変な長さの入力インターバルを設定出来そうだったが、
                    // ここが実行される前に_characterInput.Directionが入力を検知してしまう
                    // (例えばCombat終了前後でDirection入力を入れっぱなしにしていたら、IDLEステート遷移直後に_characterInputを制御するより前にmoveコマンドが実行されて動いてしまう)
                })
                .AddTo(_disposables);

            // 移動開始/終了
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ => _locomotionController.SetSpeed(_characterInput.Direction.Value))
                .AddTo(_disposables);
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ => {
                    _locomotionController.Stop();
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
                .Subscribe(_ =>
                {
                    _acceptDirectionInput = false;
                    _selectSkillController.SelectSkill(_characterInput.SelectedSkillIndex.Value);
                })
                .AddTo(_disposables);

            // スキル選択解除
            _actionStateObservable
                .OnEnd<ActionEventSkillSelect>()
                .Subscribe(_ => _selectSkillController.ResetSelection())
                .AddTo(_disposables);

            // 回転開始
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    switch (PrevState)
                    {
                        case DAMAGED:
                            _rotateAngle = -_transformController.RotationAngle;
                            break;
                        case SKILL:
                            _rotateAngle = -_selectSkillObservable.SelectedSkillRotation;
                            break;
                    }
                    _locomotionController.FixTimeRotate(_rotateAngle, _rotateTime);
                })
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventRotate>()
                .Subscribe(_ => _locomotionController.StopRotate()) // Damaged, Die割り込みなど
                .AddTo(_disposables);

            // スキル実行
            _actionStateObservable
                .OnStart<ActionEventSkill>()
                .Subscribe(_ =>
                {
                    _skillController.StartSkill(_selectSkillObservable.SelectedSkillIndex.Value);
                    _selectSkillController.ResetSelection();
                })
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventSkill>()
                .Subscribe(_ => _locomotionController.StopRotate()) // Damaged, Die割り込みなど
                .AddTo(_disposables);

            // Hexの中心にSnap
            _actionStateObservable
                .OnStart<ActionEventSnapHexCenter>()
                .Subscribe(_ => _locomotionController.SnapHexCenter())
                .AddTo(_disposables);

            // 各モーション再生
            _actionStateObservable
                .OnStart<ActionEventPlayMotion>()
                .Subscribe(ev =>
                {
                    switch (CurState)
                    {
                        case IDLE:
                        case SKILL_SELECT:
                            _memberObservable.CurMember.Value.AnimationController.Play(AnimationExtensions.IdleClip);
                            break;

                        case MOVE:
                            var direction = _characterInput.Direction.Value;
                            var euler = Quaternion.LookRotation(direction).eulerAngles.y;
                            var locomotionIndex = ((int)((euler + 22.5) / 45)) % 8;
                            _memberObservable.CurMember.Value.AnimationController.Play(AnimationExtensions.MoveClips[locomotionIndex]);
                            break;

                        case ROTATE:
                            if (_rotateAngle < 0) _memberObservable.CurMember.Value.AnimationController.Play("RotateLeft");
                            else _memberObservable.CurMember.Value.AnimationController.Play("RotateRight");
                            break;

                        case DAMAGED:
                            _memberObservable.CurMember.Value.AnimationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
            _memberChangeDisposables.Dispose();
        }
    }
}
