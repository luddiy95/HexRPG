using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Profiling;

namespace HexRPG.Battle.Enemy
{
    using Stage;
    using static ActionStateType;

    public class BaseDynamicEnemyActionStateController : MonoBehaviour, ICharacterActionStateController
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

        readonly ReactiveProperty<Vector3> _moveDirection = new ReactiveProperty<Vector3>();

        Hex _approachHex = null;
        Hex _attackableHex = null;

        List<Hex> _surroundedHexList = new List<Hex>(256);
        List<Hex> _arroundHexList = new List<Hex>(128);
        List<Hex> _enemyHexList = new List<Hex>(512);

        CompositeDisposable _disposables = new CompositeDisposable();

        CancellationTokenSource _actionCts = null;
        CancellationTokenSource _moveCts = null;

        [Header("回転")]
        [SerializeField] float MOVE_ROTATE_TIME = 0.1f;
        [SerializeField] float LOOK_ROTATE_SPEED = 240f;

        [Header("Combat攻撃範囲")]
        [SerializeField] float COMBAT_DISTANCE = 5f;

        [Header("インターバル(ms)")]
        [SerializeField] int IDLE_INTERVAL = 100;
        [SerializeField] int MOVE_SEARCH_INTERVAL = 1200; //(ms)

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
            if (_actionStateObservable.CurrentState.Value == null)
            {
                var initialState = BuildActionStates();
                SetUpControl();
                _actionStateController.SetInitialState(initialState);
            }

            _navMeshAgentController.AgentEnable = true;

            _locomotionController.ForceLookRotate(_battleObservable.PlayerLandedHex.transform.position);
            SetDestinationLandedHex();

            _actionCts = new CancellationTokenSource();
            StartActionSequence(_actionCts.Token).Forget();
        }

        void Update()
        {
            //TODO: テストコード
            if (Input.GetKeyDown(KeyCode.A))
            {
                _actionStateController.Execute(new Command { Id = "combat" });
            }
        }

        ActionState BuildActionStates()
        {
            // 「Rotate, Damaged, DieはExecuteTransactionで遷移」

            var idle = NewState(IDLE)
                .AddEvent(new ActionEventPlayMotion(0f))
                .AddEvent(new ActionEventCancel("move", MOVE))
                .AddEvent(new ActionEventCancel("rotate", ROTATE))
                .AddEvent(new ActionEventCancel("damaged", DAMAGED))
            ;

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

            return idle;
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
                        _moveDirection.SetValueAndForceNotify(_transformController.Position.GetRelativePosXZ(_navMeshAgentController.CurSteeringTargetPos));
                    }
                })
                .AddTo(_disposables);

            _moveDirection
                .Skip(1)
                .Subscribe(_ =>
                {
                    _actionStateController.Execute(new Command { Id = "move" });
                })
                .AddTo(_disposables);

            // NavMeshの状況が変わったら即経路探索
            _battleObservable.OnUpdateNavMesh
                .Where(_ => _moveCts != null)
                .Subscribe(_ => SearchDestinationAfterUpdateNavMesh(_moveCts.Token).Forget())
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
                    _actionCts = new CancellationTokenSource();
                    StartActionSequenceAfterDamaged(_actionCts.Token).Forget();
                })
                .AddTo(this);

            // Die
            _dieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ =>
                {
                    AllTokenCancel(); // Sequence中断

                    DeleteDestination();
                    _navMeshAgentController.AgentEnable = false;

                    _actionStateController.ExecuteTransition(DIE);
                })
                .AddTo(_disposables);

            ////// ステートでの詳細処理 //////

            // 移動開始/終了
            _actionStateObservable
                .OnStart<ActionEventMove>()
                .Subscribe(_ =>
                {
                    if (_moveDirection.Value.sqrMagnitude > 0.1)
                        _locomotionController.FixTimeLookRotate(_transformController.Position + _moveDirection.Value, MOVE_ROTATE_TIME);
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
                    _rotateAngle = _locomotionController.LookRotate(_battleObservable.OnPlayerSpawn.Value.TransformController.Position, LOOK_ROTATE_SPEED);
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
            _actionStateController.ExecuteTransition(IDLE);

            while (true)
            {
                // Idle
                await UniTask.Delay(IDLE_INTERVAL, cancellationToken: token);

                // Move
                _attackableHex = null;
                _approachHex = null;

                _moveCts = new CancellationTokenSource();
                var breakStateType = await StartMoveSequence(_moveCts.Token);
                MoveTokenCancel();

                switch (breakStateType)
                {
                    case IDLE:
                        //! どこにも移動できずlandedHexに居続けるしかない場合は、idle -> rotate -> idle遷移が頻繁に行われるので、その遷移でも違和感のないモーションにする必要がある
                        _actionStateController.Execute(new Command { Id = "rotate" });
                        await _locomotionObservable.OnFinishRotate.ToUniTask(useFirstValue: true, cancellationToken: token);
                        _actionStateController.Execute(new Command { Id = "idle" });
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
                if (_navMeshAgentController.CurDestination != null) enemyDestinationHexList.Remove(_navMeshAgentController.CurDestination.Value);

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
                        return IDLE; //! 現在のLandedHexから動きようがなくてかつhexの中心にいる
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
                SearchDestination();
            }
        }

        void SearchDestination()
        {
            var landedHex = _transformController.GetLandedHex();
            var relativeDirFromHexCenter = _transformController.Position.GetRelativePosXZ(landedHex.transform.position);

            var playerLandedHex = _battleObservable.PlayerLandedHex;
            var distance2FromPlayerHex = landedHex.GetDistance2XZ(playerLandedHex);

            var enemyDestinationHexList = new List<Hex>(_battleObservable.EnemyDestinationHexList);
            if (_navMeshAgentController.CurDestination != null) enemyDestinationHexList.Remove(_navMeshAgentController.CurDestination.Value);

            if (_attackableHex == landedHex || _approachHex == landedHex) return; //! 目的地のhexにいたら状況が変わってもDestinationを変更しない

            // PlayerからCOMBAT_DISTANCEの距離以内のHexでEnemyが移動出来るHexで最もPlayerに近いHexへ移動
            TransformExtensions.GetSurroundedHexList(playerLandedHex, COMBAT_DISTANCE, ref _surroundedHexList);
            _enemyHexList.Clear();
            if (_surroundedHexList.Contains(landedHex)) _enemyHexList.Add(landedHex);
            var surroundedEnemyHexList = _surroundedHexList
                .Where(hex =>
                    hex.IsPlayerHex == false
                    && _navMeshAgentController.IsExistPath(hex.transform.position)
                    && hex.GetDistance2XZ(playerLandedHex) + 0.1f < distance2FromPlayerHex //! 現在のLandedHexよりPlayerLandedHexに近いHex(止まっている状態から全く同じ距離のhexへ進まないように)
                    && enemyDestinationHexList.Contains(hex) == false);
            _enemyHexList.AddRange(surroundedEnemyHexList);

            var attackableHex = _enemyHexList.OrderBy(hex => hex.GetDistance2XZ(playerLandedHex)).FirstOrDefault();

            if (attackableHex != null)
            {
                _approachHex = null;

                //! 既に_attackableHexがあり、Liberateにより状況が変わっておらず(_attackableHexへ変わらず移動可能)、現在の_attackableHex ~ PlayerLandedHex間の距離より短くならない場合は更新しない
                if (_attackableHex != null && _enemyHexList.Contains(_attackableHex)
                    && attackableHex.GetDistance2XZ(playerLandedHex) + 0.1f >= _attackableHex.GetDistance2XZ(playerLandedHex))
                {
                    return; // 既に_attackableHexでSetDestinationされているはず
                }

                _attackableHex = attackableHex;
                SetDestination(_attackableHex);
                return; //! Playerに攻撃があたるHexへ移動
            }

            _enemyHexList.Clear();
            _enemyHexList.Add(landedHex);
            int radius = 1;
            while (true)
            {
                _stageController.GetArroundHexList(landedHex, radius, ref _arroundHexList);
                var aroundEnemyHexList = _arroundHexList
                    .Where(hex =>
                        hex.IsPlayerHex == false
                        && _navMeshAgentController.IsExistPath(hex.transform.position)
                        && hex.GetDistance2XZ(playerLandedHex) + 0.1f < distance2FromPlayerHex //! 現在のLandedHexよりPlayerLandedHexに近いHex(止まっている状態から全く同じ距離のhexへ進まないように)
                        && enemyDestinationHexList.Contains(hex) == false);

                _enemyHexList.AddRange(aroundEnemyHexList);

                if (_arroundHexList.Contains(playerLandedHex)) break;
                ++radius;
            }

            var approachHex = _enemyHexList.OrderBy(hex => hex.GetDistance2XZ(playerLandedHex)).FirstOrDefault();

            // 毎フレームの評価ですぐにROTATEかIDLEでreturnされる
            if (approachHex == null) // landedHexに留まることも許されない
            {
                _approachHex = landedHex;
                return;
            }
            if (approachHex == landedHex && CurState == IDLE)
            {
                if (relativeDirFromHexCenter.sqrMagnitude < 0.1f)
                {
                    _approachHex = landedHex;
                    return; // hexの真ん中で静止している状態で現在のlandedHexで動きようがない場合はMOVEに遷移させない
                }
            }

            //! 既に_approachHexがあり、現在の_approachHex ~ PlayerLandedHex間の距離より短くならない場合は更新しない
            if (_approachHex != null && _enemyHexList.Contains(_approachHex) && approachHex.GetDistance2XZ(playerLandedHex) + 0.1f >= _approachHex.GetDistance2XZ(playerLandedHex))
            {
                //SetDestination(_approachHex); // 既に_approachHexでSetDestinationされているはずなのでいらない
                return; // 現在の_approachHexのまま続行
            }

            _approachHex = approachHex;

            SetDestination(_approachHex);
            return; //! なるべくPlayerに近付こうとする
        }

        async UniTaskVoid SearchDestinationAfterUpdateNavMesh(CancellationToken token)
        {
            await UniTask.Yield(token); // NavMeshSurfaceがNavMeshを更新するのを待つ
            SearchDestination();
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

        void MoveTokenCancel()
        {
            _moveCts?.Cancel();
            _moveCts?.Dispose();
            _moveCts = null;
        }

        void AllTokenCancel()
        {
            MoveTokenCancel();

            _actionCts?.Cancel();
            _actionCts?.Dispose();
            _actionCts = null;
        }

        void OnDestroy()
        {
            _disposables.Dispose();
            AllTokenCancel();
        }
    }
}
