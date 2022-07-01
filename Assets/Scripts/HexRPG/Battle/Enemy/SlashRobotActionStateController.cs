using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Enemy
{
    using Stage;
    using static ActionStateType;

    public class SlashRobotActionStateController : MonoBehaviour, ICharacterActionStateController
    {
        IUpdateObservable _updateObservable;
        IBattleObservable _battleObservable;
        IStageController _stageController;

        ITransformController _transformController;
        IAnimationController _animationController;

        INavMeshAgentController _navMeshAgentController;

        ILocomotionObservable _locomotionObservable;
        ILocomotionController _locomotionController;

        IActionStateController _actionStateController;
        IActionStateObservable _actionStateObservable;

        IDamageApplicable _damagedApplicable;
        IDieObservable _dieObservable;

        ICombatController _combatController;
        ICombatObservable _combatObservable;

        int _rotateAngle = 0;

        readonly IReactiveProperty<Vector3> _moveDirection = new ReactiveProperty<Vector3>();

        Hex _approachHex = null;
        Hex _attackableHex = null;

        CompositeDisposable _disposables = new CompositeDisposable();

        CancellationTokenSource _actionCancellationTokenSource = new CancellationTokenSource();
        CancellationTokenSource _moveCancellationTokenSource = new CancellationTokenSource();

        const float COMBAT_DISTANCE = 5f;
        const int MOVE_SEARCH_INTERVAL = 1200; //(ms)

        ActionStateType CurState => _actionStateObservable.CurrentState.Value.Type;

        [Inject]
        public void Construct(
            IUpdateObservable updateObservable,
            IBattleObservable battleObservable,
            IStageController stageController,
            ITransformController transformController,
            IAnimationController animationController,
            INavMeshAgentController navMeshAgentController,
            ILocomotionController locomotionController,
            ILocomotionObservable locomotionObservable,
            IActionStateController actionStateController,
            IActionStateObservable actionStateObservable,
            IDamageApplicable damageApplicable,
            IDieObservable dieObservable,
            ICombatController combatController,
            ICombatObservable combatObservable
        )
        {
            _updateObservable = updateObservable;
            _battleObservable = battleObservable;
            _stageController = stageController;
            _transformController = transformController;
            _animationController = animationController;
            _navMeshAgentController = navMeshAgentController;
            _locomotionController = locomotionController;
            _locomotionObservable = locomotionObservable;
            _actionStateController = actionStateController;
            _actionStateObservable = actionStateObservable;
            _damagedApplicable = damageApplicable;
            _dieObservable = dieObservable;
            _combatController = combatController;
            _combatObservable = combatObservable;
        }

        void ICharacterActionStateController.Init()
        {
            BuildActionStates();
            SetUpControl();

            _locomotionController.ForceLookRotate(_battleObservable.PlayerLandedHex.transform.position);
            SetDestinationLandedHex();

            StartActionSequence(_actionCancellationTokenSource.Token).Forget();
        }

        void Update()
        {
            //TODO: テストコード
            if (Input.GetKeyDown(KeyCode.A))
            {
                _actionStateController.Execute(new Command { Id = "combat" });
            }
        }

        void BuildActionStates()
        {
            // 「Rotate, Damaged, DieはExecuteTransactionで遷移」

            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;
            _actionStateController.SetInitialState(idle);

            NewState(MOVE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventMove(0f))
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("move", MOVE, passEndNotification: true))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;

            NewState(ROTATE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventRotate())
                .AddEvent(new ActionEventCancel("idle", IDLE))
                .AddEvent(new ActionEventCancel("combat", COMBAT))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
                ;

            NewState(DAMAGED)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("finishDamaged", IDLE))
                ;

            NewState(COMBAT)
                .AddEvent(new ActionEventCombat())
                .AddEvent(new ActionEventCancel("finishCombat", IDLE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
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
            // Move
            _updateObservable.OnUpdate((int)UPDATE_ORDER.MOVE)
                .Subscribe(_ =>
                {
                    _navMeshAgentController.NextPosition = _transformController.Position;
                    if (!_navMeshAgentController.IsStopped)
                    {
                        // 経路探索前のDelay中も移動
                        _moveDirection.Value = _transformController.Position.GetRelativePosXZ(_navMeshAgentController.CurSteeringTargetPos);
                    }
                })
                .AddTo(_disposables);

            _moveDirection
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "move" });
                })
                .AddTo(_disposables);

            // Damaged
            _damagedApplicable.OnHit
                .Subscribe(hitData =>
                {
                    var hitType = hitData.HitType;
                    if (hitType == HitType.WEAK || hitType == HitType.CRITICAL)
                    {
                        AllTokenCancel(); // Sequence中断

                        var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(_transformController.GetLandedHex().transform.position);
                        if (relativeDirFromHexCenter.sqrMagnitude < 0.1f) //? 0.1f > 「moveSpeed( /s) * 0.016(s)」
                        {
                            SetDestinationLandedHex();
                        }
                        else
                        {
                            DeleteDestination();
                        }

                        _actionStateController.Execute(new Command { Id = "damaged" });
                    }
                })
                .AddTo(_disposables);

            _animationController.OnFinishDamaged
                .Subscribe(_ =>
                {
                    _actionCancellationTokenSource = new CancellationTokenSource();
                    StartActionSequenceAfterDamaged(_actionCancellationTokenSource.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    AllTokenCancel(); // Sequence中断

                    DeleteDestination();
                    _actionStateController.ExecuteTransition(DIE);
                })
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // 移動開始/終了
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ =>
                {
                    if (_moveDirection.Value.sqrMagnitude > 0.1) _locomotionController.FixTimeLookRotate(_transformController.Position + _moveDirection.Value, 0.1f);
                    _locomotionController.SetSpeed(_moveDirection.Value);
                })
                .AddTo(_disposables);
            _actionStateObservable
                .OnEnd<ActionEventMove>()
                .Subscribe(_ =>
                {
                    _locomotionController.StopRotate();
                    _locomotionController.Stop();
                }).AddTo(_disposables);

            // Rotate
            _actionStateObservable
                .OnStart<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    _rotateAngle = _locomotionController.LookRotate(_battleObservable.OnPlayerSpawn.Value.TransformController.Position, 240f);
                })
                .AddTo(_disposables);

            _actionStateObservable
                .OnEnd<ActionEventRotate>()
                .Subscribe(_ =>
                {
                    _locomotionController.StopRotate();
                })
                .AddTo(_disposables);

            // Combat
            _actionStateObservable
                .OnStart<ActionEventCombat>()
                .Subscribe(_ =>
                {
                    _combatController.Combat();
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

                        case ROTATE:
                            if (_rotateAngle < 0) _animationController.Play("RotateLeft");
                            else _animationController.Play("RotateRight");
                            break;

                        case MOVE:
                            _animationController.Play("Movefwd");
                            break;

                        case DAMAGED:
                            _animationController.Play("Damaged");
                            break;
                    }
                })
                .AddTo(_disposables);
        }

        async UniTaskVoid StartActionSequence(CancellationToken token)
        {
            while (true)
            {
                // Idle
                await UniTask.Delay(100, cancellationToken: token);

                // Move
                _attackableHex = null;
                _approachHex = null;

                _moveCancellationTokenSource = new CancellationTokenSource();
                var breakStateType = await StartMoveSequence(_moveCancellationTokenSource.Token);
                _moveCancellationTokenSource.Cancel();

                switch (breakStateType)
                {
                    case IDLE:
                        _actionStateController.Execute(new Command { Id = "idle" });
                        break;
                    case ROTATE:
                        if (CurState == MOVE) // 動いていた状態から動きようがない状態になったときの最初の一回(Move -> Idle)のみPlayerの方を向く
                        {
                            _actionStateController.Execute(new Command { Id = "rotate" });
                            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                            _actionStateController.Execute(new Command { Id = "idle" });
                        }
                        break;
                    case COMBAT:
                        _actionStateController.Execute(new Command { Id = "rotate" });
                        await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);

                        // Combat
                        _actionStateController.Execute(new Command { Id = "combat" });
                        await _combatObservable.OnFinishCombat.ToUniTask(useFirstValue: true, cancellationToken: token);
                        _actionStateController.Execute(new Command { Id = "finishCombat" });

                        await UniTask.Delay(1000, cancellationToken: token); // Delayが小さかったらidle->rotate遷移中にidle割り込みの可能性がある

                        _actionStateController.Execute(new Command { Id = "rotate" });
                        await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                        _actionStateController.Execute(new Command { Id = "idle" }); break;
                }
            }
        }

        async UniTask<ActionStateType> StartMoveSequence(CancellationToken token)
        {
            StartSearchDestinationIterater(token).Forget(); // 一定時間ごとに経路探索

            while (true)
            {
                var landedHex = _transformController.GetLandedHex();
                var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

                var enemyDestinationHexList = new List<Hex>(_battleObservable.EnemyDestinationHexList);
                //! 「向かう先がlandedHex or landedHexの真ん中で静止している」 == enemyDestinationHexListからlandedHexをremoveして良い
                if(_navMeshAgentController.CurDestination.Value == landedHex) enemyDestinationHexList.Remove(landedHex);

                // DestinationやpathがLiberateによって状況が変わった
                if (!_navMeshAgentController.IsStopped)
                {
                    var directionHex = TransformExtensions.GetLandedHex(_transformController.Position + _moveDirection.Value.normalized * 0.1f);
                    if (directionHex.IsPlayerHex)
                    {
                        DeleteDestination();
                        return IDLE;
                    }
                }

                // Playerが攻撃範囲内
                if (_attackableHex == landedHex)
                {
                    if (relativeDirFromHexCenter.sqrMagnitude < 0.1f) //? 0.1f > 「moveSpeed( /s) * 0.016(s)」
                    {
                        SetDestinationLandedHex();
                        return COMBAT; //! playerが攻撃範囲内でかつhexの中心に着いた
                    }

                    if (enemyDestinationHexList.Contains(landedHex))
                    {
                        DeleteDestination();
                        return IDLE; //! 状況が変わり目的地に別のEnemyが居座っていた
                    }
                }

                // 現在のHexから動きようがない
                if (_approachHex == landedHex)
                {
                    // Hexの中心にいるかどうか
                    if (relativeDirFromHexCenter.sqrMagnitude < 0.1f)
                    {
                        SetDestinationLandedHex();
                        return ROTATE; //! 現在のLandedHexから動きようがなくてかつhexの中心にいる
                    }

                    if (enemyDestinationHexList.Contains(landedHex))
                    {
                        DeleteDestination();
                        return IDLE; //! 状況が変わり目的地に別のEnemyが居座っていた
                    }
                }

                await UniTask.WaitForEndOfFrame(cancellationToken: token);
                continue;
            };
        }

        async UniTaskVoid StartSearchDestinationIterater(CancellationToken token)
        {
            while (true)
            {
                await UniTask.Delay(MOVE_SEARCH_INTERVAL, cancellationToken: token);

                var landedHex = _transformController.GetLandedHex();
                var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

                var playerLandedHex = _battleObservable.PlayerLandedHex;
                var distance2FromPlayerHex = landedHex.GetDistance2XZ(playerLandedHex);

                var enemyDestinationHexList = new List<Hex>(_battleObservable.EnemyDestinationHexList);
                //! 「向かう先がlandedHex or landedHexの真ん中で静止している」 == enemyDestinationHexListからlandedHexをremoveして良い
                if (_navMeshAgentController.CurDestination.Value == landedHex) enemyDestinationHexList.Remove(landedHex);

                if (_attackableHex == landedHex || _approachHex == landedHex) continue; //! 目的地のhexにいたら状況が変わってもDestinationを変更しない

                // PlayerからCOMBAT_DISTANCEの距離以内のHexでEnemyが移動出来るHexで最も近いHexへ移動
                _attackableHex = TransformExtensions.GetSurroundedHexList(playerLandedHex, COMBAT_DISTANCE)
                    .Where(hex => hex.IsPlayerHex == false && _navMeshAgentController.IsExistPath(hex.transform.position))
                    .Where(hex => enemyDestinationHexList.Contains(hex) == false)
                    .OrderBy(hex => hex.GetDistance2XZ(landedHex))
                    .FirstOrDefault();

                if (_attackableHex != null)
                {
                    SetDestination(_attackableHex);
                    continue; //! Playerに攻撃があたるHexへ移動
                }

                int radius = 0;
                var enemyHexList = new List<Hex>();
                while (true)
                {
                    var aroundHexList = _stageController.GetAroundHexList(landedHex, radius);
                    var aroundEnemyHexList = aroundHexList
                        .Where(hex =>
                            hex.IsPlayerHex == false
                            && _navMeshAgentController.IsExistPath(hex.transform.position)
                            && hex.GetDistance2XZ(playerLandedHex) <= distance2FromPlayerHex) // 現在のPlayerLandedHexへの距離より短くなる
                        .Where(hex => enemyDestinationHexList.Contains(hex) == false);
                    enemyHexList.AddRange(aroundEnemyHexList);
                    if (aroundHexList.Contains(playerLandedHex)) break;
                    ++radius;
                }

                _approachHex = enemyHexList.OrderBy(hex => hex.GetDistance2XZ(playerLandedHex)).FirstOrDefault();

                if (_approachHex == null) // landedHexに留まることも許されない
                {
                    _approachHex = landedHex;
                    continue;
                }

                if(_approachHex == landedHex && CurState == IDLE)
                {
                    if (relativeDirFromHexCenter.sqrMagnitude < 0.1f)
                    {
                        continue; // hexの真ん中で静止している状態で現在のlandedHexで動きようがない場合はMOVEに遷移させない
                    }
                }

                SetDestination(_approachHex);
                continue; //! なるべくPlayerに近付こうとする
            }
        }

        void SetDestination(Hex destination)
        {
            _navMeshAgentController.SetDestination(destination);
            _navMeshAgentController.IsStopped = false;
        }

        void SetDestinationLandedHex()
        {
            _navMeshAgentController.IsStopped = true;
            _navMeshAgentController.SetDestination(_transformController.GetLandedHex());
        }

        void DeleteDestination()
        {
            _navMeshAgentController.IsStopped = true;
            _navMeshAgentController.SetDestination(null);
        }

        async UniTaskVoid StartActionSequenceAfterDamaged(CancellationToken token)
        {
            _actionStateController.Execute(new Command { Id = "finishDamaged" });

            await UniTask.DelayFrame(1, cancellationToken: token);

            _actionStateController.Execute(new Command { Id = "rotate" });
            await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
            _actionStateController.Execute(new Command { Id = "idle" });

            StartActionSequence(token).Forget();
        }

        void AllTokenCancel()
        {
            _moveCancellationTokenSource.Cancel();
            _actionCancellationTokenSource.Cancel();
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            _moveCancellationTokenSource.Dispose();
            _actionCancellationTokenSource.Dispose();
        }
    }
}
