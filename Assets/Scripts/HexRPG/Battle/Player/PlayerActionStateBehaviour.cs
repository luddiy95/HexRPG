using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using Zenject;

namespace HexRPG.Battle.Player
{
    using static ActionStateType;

    public class PlayerActionStateBehaviour : MonoBehaviour, ICharacterActionStateController
    {
        ITransformController _transformController;

        ILocomotionController _locomotionController;
        ILocomotionObservable _locomotionObservable;

        ICharacterInput _characterInput;

        ICameraRotateController _cameraRotateController;
        ICameraRotateObservable _cameraRotateObservable;

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

        float _rotateTime = 0.2f; //! durationより短いとダメ(予期せぬ遷移中割り込み) & 固定時間Rotateでないとだめ -> curAnimationで遷移中割り込み可能なので大丈夫そう

        CompositeDisposable _disposables = new CompositeDisposable();
        CompositeDisposable _memberChangeDisposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            ITransformController transformController,
            ILocomotionController locomotionController,
            ILocomotionObservable locomotionObservable,
            ICharacterInput characterInput,
            ICameraRotateController cameraRotateController,
            ICameraRotateObservable cameraRotateObservable,
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
            _cameraRotateController = cameraRotateController;
            _cameraRotateObservable = cameraRotateObservable;
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
            var initialState = BuildActionStates();

            // Member変更
            _memberObservable.CurMember
                .Subscribe(member =>
                {
                    if (_actionStateObservable.CurrentState.Value == null)
                    {
                        SetUpControl();
                        _actionStateController.SetInitialState(initialState);

                        this.FixedUpdateAsObservable()
                            .Subscribe(_ =>
                            {
                                //TODO: 【ここから】移動中にモーションが再生されない
                                if (_characterInput.Direction.sqrMagnitude > 0.1)
                                {
                                    var canMoveState = (CurState == IDLE || CurState == MOVE || CurState == SKILL_SELECT);
                                    if (canMoveState && _acceptDirectionInput)
                                    {
                                        _actionStateController.Execute(new Command { Id = "move" });

                                        var direction = _characterInput.Direction;
                                        _locomotionController.FixTimeLookRotate(_transformController.Position + _characterInput.Direction, 0.05f);

                                        _locomotionController.SetSpeed(direction);
                                    }
                                }
                                else
                                {
                                    if (CurState == MOVE) _actionStateController.Execute(new Command { Id = "stop" });

                                    // Skill選択ステートに遷移した後、一度Direction入力がzeroにならないとDirection入力による移動を受け付けない
                                    if (CurState == SKILL_SELECT) _acceptDirectionInput = true;
                                }
                            })
                            .AddTo(_disposables);
                    }
                    else
                    {
                        _actionStateController.ExecuteTransition(IDLE);
                    }

                    _memberChangeDisposables.Clear();
                    member.AnimationController.OnFinishDamaged
                        .Where(_ => CurState == DAMAGED)
                        .Subscribe(_ =>
                        {
                            _actionStateController.Execute(new Command { Id = "finishDamaged" });
                        })
                        .AddTo(_memberChangeDisposables);

                    member.DieObservable.IsDead
                        .Where(isDead => isDead)
                        .Subscribe(_ => _actionStateController.ExecuteTransition(DIE))
                        .AddTo(_memberChangeDisposables);
                })
                .AddTo(_disposables);
        }

        ActionState BuildActionStates()
        {
            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventIdle(0f))
                .AddEvent(new ActionEventCancel("move", 0.15f, MOVE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", 0.15f, COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", 0.15f, SKILL_SELECT))
                ;

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("stop", IDLE))
                .AddEvent(new ActionEventCancel("move", MOVE, passEndNotification: true))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("skillSelect", SKILL_SELECT))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
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

            return idle;
        }

        void SetUpControl()
        {
            ////// Player入力などによる状態遷移 //////
            /// ※ UPDATE_ORDER.INPUTで同時にInputされた場合(例えばDirection, Combat)、↓のSubscribe記述順にCommandが実行されるため記述の順序に注意

            // Damaged
            _damagedApplicable.OnHit //! PlayerはHitTypeに依らずDamagedモーションを取る
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if (hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        _actionStateController.Execute(new Command { Id = "damaged" });
                    }
                })
                .AddTo(_disposables);

            // CameraRotate
            _characterInput.CameraRotateDir
                .Where(_ => _cameraRotateObservable.IsCameraRotating == false)
                .Where(_ => CurState == IDLE || CurState == MOVE || CurState == SKILL_SELECT)
                .Subscribe(rotateDir =>
                {
                    _cameraRotateController.FixTimeCameraRotate(rotateDir, 0.25f);
                    _actionStateController.Execute(new Command { Id = "skillCancel" });
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
                .Skip(1)
                .Where(_ => _cameraRotateObservable.IsCameraRotating == false) // カメラ回転中はSkill選択できない
                .Where(_ => CurState == IDLE || CurState == MOVE || CurState == SKILL_SELECT)
                .Subscribe(index =>
                {
                    var skillList = _memberObservable.CurMember.Value.SkillSpawnObservable.SkillList;
                    if (index > skillList.Count - 1) return;
                    if (skillList[index].SkillSetting.Cost > _memberObservable.CurMember.Value.SkillPoint.Current.Value) return;
                    _actionStateController.Execute(new Command { Id = "skillSelect" });
                })
                .AddTo(_disposables);

            // Skillキャンセル時
            _characterInput.OnSkillCancel
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
                .Where(_ => PrevState == DAMAGED || PrevState == SKILL)
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "idle" });
                })
                .AddTo(_disposables);

            // Skill終了
            _skillObservable.OnFinishSkill
                .Where(_ => CurState == SKILL)
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "finishSkill" });
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

            // 移動終了
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ =>
                {
                    _locomotionController.StopRotate();
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

                    _locomotionController.SnapHexCenter();
                    _locomotionController.ForceRotate(0);
                    _selectSkillController.SelectSkill(_characterInput.SelectedSkillIndex.Value);
                })
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
                    _skillController.StartSkill(_selectSkillObservable.SelectedSkillIndex.Value);
                    _selectSkillController.ResetSelection();
                })
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventSkill>()
                .Subscribe(_ => _locomotionController.StopRotate()) // Damaged, Die割り込みなど
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
                            _memberObservable.CurMember.Value.AnimationController.Play("Movefwd"); break;
                        case DAMAGED:
                            _memberObservable.CurMember.Value.AnimationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        void OnDestroy()
        {
            _memberChangeDisposables?.Dispose();
            _disposables.Dispose();
        }
    }
}
