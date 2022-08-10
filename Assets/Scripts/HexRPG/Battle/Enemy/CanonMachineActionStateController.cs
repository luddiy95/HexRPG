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

        ITransformController _transformController;
        IAnimationController _animationController;

        INavMeshAgentController _navMeshAgentController;

        ILocomotionObservable _locomotionObservable;
        ILocomotionController _locomotionController;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IDamageApplicable _damagedApplicable;
        IDieObservable _dieObservable;

        ISkillController _skillController;
        ISkillObservable _skillObservable;

        CompositeDisposable _disposables = new CompositeDisposable();
        CancellationTokenSource _cts = null;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            ITransformController transformController,
            IAnimationController animationController,
            INavMeshAgentController navMeshAgentController,
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
            _transformController = transformController;
            _animationController = animationController;
            _navMeshAgentController = navMeshAgentController;
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
            var initialState = BuildActionStates();
            SetUpControl();
            _actionStateController.SetInitialState(initialState);

            _navMeshAgentController.AgentEnable = true;

            _locomotionController.ForceLookRotate(_battleObservable.PlayerLandedHex.transform.position);
            _navMeshAgentController.SetDestination(_transformController.GetLandedHex());

            _cts = new CancellationTokenSource();
            StartSequence(_cts.Token).Forget();
        }

        void Update()
        {
            //TODO: テストコード
            if (Input.GetKeyDown(KeyCode.A))
            {
                //_actionStateController.Execute(new Command { Id = "skill" });
            }
        }

        ActionState BuildActionStates()
        {
            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("rotate", 0, ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

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

            return idle;
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
                        TokenCancel(); // Sequence中断
                        _actionStateController.Execute(new Command { Id = "damaged" });
                    }
                })
                .AddTo(_disposables);

            _animationController.OnFinishDamaged
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "finishDamaged" });

                    // Sequence再開
                    _cts = new CancellationTokenSource();
                    StartSequence(_cts.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    TokenCancel(); // Sequence中断

                    _navMeshAgentController.SetDestination(null);
                    _navMeshAgentController.AgentEnable = false;

                    _actionStateController.ExecuteTransition(DIE);
                })
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // Rotate
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ => _locomotionController.LookRotate(_battleObservable.OnPlayerSpawn.Value.TransformController.Position, 240f))
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
                await UniTask.Delay(5000, cancellationToken: token);

                // Playerの方へ回転
                _actionStateController.Execute(new Command { Id = "rotate" });
                await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                // Skill
                _actionStateController.Execute(new Command { Id = "skill" });
                await _skillObservable.OnFinishSkill.ToUniTask(useFirstValue: true, cancellationToken: token);
                _actionStateController.Execute(new Command { Id = "finishSkill" });
            }
        }

        void TokenCancel()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            TokenCancel();
        }
    }
}
