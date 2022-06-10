using UnityEngine;
using Zenject;
using UniRx;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    using static ActionStateType;

    public class CanonMachineActionStateController : MonoBehaviour, ICharacterActionStateController
    {
        IBattleObservable _battleObservable;

        IAnimationController _animationController;

        ILocomotionObservable _locomotionObservable;
        ILocomotionController _locomotionController;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IDamageApplicable _damagedApplicable;
        IDieObservable _dieObservable;

        ISkillController _skillController;
        ISkillObservable _skillObservable;

        CompositeDisposable _disposables = new CompositeDisposable();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            IAnimationController animationController,
            ILocomotionController locomotionController,
            ILocomotionObservable locomotionObservable,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IDamageApplicable damageApplicable,
            IDieObservable dieObservable,
            ISkillController skillController,
            ISkillObservable skillObservable
        )
        {
            _battleObservable = battleObservable;
            _animationController = animationController;
            _locomotionController = locomotionController;
            _locomotionObservable = locomotionObservable;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _damagedApplicable = damageApplicable;
            _dieObservable = dieObservable;
            _skillController = skillController;
            _skillObservable = skillObservable;
        }

        void ICharacterActionStateController.Init()
        {
            BuildActionStates();
            SetUpControl();

            _locomotionController.ForceLookRotate(_battleObservable.PlayerLandedHex.transform.position);
            StartSequence(_cancellationTokenSource.Token).Forget();
        }

        void Update()
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
                .AddEvent(new ActionEventCancel("rotate", 0, ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;
            _actionStateController.SetInitialState(idle);

            NewState(ROTATE)
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("skill", 0, SKILL))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
                ;

            NewState(SKILL)
                .AddEvent(new ActionEventSkill())
                .AddEvent(new ActionEventCancel("finishSkill", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
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
            // Damaged
            _damagedApplicable.OnHit
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if(hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        _cancellationTokenSource.Cancel(); // Sequence中断
                        _actionStateController.Execute(new Command { Id = "damaged" });
                    }
                })
                .AddTo(_disposables);

            _animationController.OnFinishDamaged
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "finishDamaged" });

                    // Sequence再開
                    _cancellationTokenSource = new CancellationTokenSource();
                    StartSequence(_cancellationTokenSource.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    _cancellationTokenSource.Cancel(); // Sequence中断
                    _actionStateController.ExecuteTransition(DIE);
                })
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // Rotate
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ => _locomotionController.LookRotate(_battleObservable.PlayerLandedHex.transform.position, 240f))
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventRotate>()
                .Subscribe(_ => _locomotionController.StopRotate())
                .AddTo(_disposables);

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

                        case DAMAGED:
                            _animationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        async UniTaskVoid StartSequence(CancellationToken token)
        {
            while (true)
            {
                // Idle
                await UniTask.Delay(2000, cancellationToken: token);

                // Playerの方へ回転
                _actionStateController.Execute(new Command { Id = "rotate" });
                await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                // Skill
                _actionStateController.Execute(new Command { Id = "skill" });
                await _skillObservable.OnFinishSkill.ToUniTask(useFirstValue: true, cancellationToken: token);
                _actionStateController.Execute(new Command { Id = "finishSkill" });
            }
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
