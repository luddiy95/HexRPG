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

        CompositeDisposable _disposables = new CompositeDisposable();
        CompositeDisposable _memberChangeDisposables = new CompositeDisposable();

        public PlayerActionStateController(
            ILocomotionController locomotionController,
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
            _locomotionController = locomotionController;
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

        void IInitializable.Initialize()
        {
            BuildActionStates();
            SetUpControl();
        }

        void BuildActionStates()
        {
            var idle = NewState(IDLE)
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

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                // IDLEに戻る
                ;

            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                .AddEvent(new ActionEventCancel("combat", COMBAT, passEndNotification: true))
                // IDLEに戻る
                ;

            //TODO: Skill選択中に動いたときIndicatorが解除されるかされないかは未定だが、とりあえずSkill選択時に止まる(Idle)ようにする
            //TODO: MoveしたままSKILL_SELECT遷移したときは、次に初めてDirection.magnitude = 0になった後、初めてDirection.magnitude > 0になったときにCancelされる
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
            /// ※ UPDATE_ORDER.INPUTで同時にInputされた場合(例えばDirection, Combat)、↓のSubscribe記述順にCommandが実行されるため記述の順序に注意

            // Member変更
            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(member =>
                {
                    _actionStateController.ExecuteTransition(IDLE);

                    _memberChangeDisposables.Clear();
                    member.AnimationController.OnFinishDamaged
                        .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                        .AddTo(_memberChangeDisposables);
                })
                .AddTo(_disposables);

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
                        _actionStateController.Execute(new Command { Id = "move" });
                    }
                    else
                    {
                        _actionStateController.Execute(new Command { Id = "stop" });
                    }
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
                .Where(_ => _actionStateObservable.CurrentState.Value.Type == COMBAT)
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
            _skillObservable.OnFinishSkill
                .Where(_ => _actionStateObservable.CurrentState.Value.Type == SKILL)
                .Subscribe(_ => _actionStateController.ExecuteTransition(IDLE))
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // Idle遷移時
            _actionStateObservable
                .OnStart<ActionEventIdle>()
                .Subscribe(_ =>
                {
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
                    switch (_actionStateObservable.CurrentState.Value.Type)
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
