using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;

namespace HexRPG.Battle
{
    public struct Command
    {
        public string Id { get; set; }

        public Vector3 Direction { get; set; }

        public bool IsEmpty => Id == string.Empty;

        public static Command Empty { get; } = new Command { Id = string.Empty };
    }

    public class ActionStateController : AbstractCustomComponent, IActionStateController, IActionStateObservable, IActionEventNotifier
    {
        IReadOnlyReactiveProperty<ActionState> IActionStateObservable.CurrentState => _currentState;
        IReadOnlyReactiveProperty<Command> IActionStateObservable.ExecutedCommand => _executedCommand;
        ICollection<ActionState> IActionStateObservable.StateHistory => _stateHistory;

        IReactiveProperty<ActionState> _currentState = new ReactiveProperty<ActionState>();
        readonly IReactiveProperty<Command> _executedCommand = new ReactiveProperty<Command>();
        readonly List<ActionState> _stateHistory = new List<ActionState>();

        readonly List<ActionState> _actionStates = new List<ActionState>();

        readonly ISubject<ActionState> _onEnterState = new Subject<ActionState>();
        readonly ISubject<ActionState> _onExitState = new Subject<ActionState>();

        readonly Dictionary<Type, object[]> _eventStreamPool = new Dictionary<Type, object[]>();

        readonly StatePosition[] _eventPositions = new StatePosition[100];

        readonly List<ActionEventCancel> _activeCancelEvents = new List<ActionEventCancel>();

        ActionState _initialState = null;
        ActionState _nextState = null;
        Command _requestCommand = Command.Empty;
        float _currentStateTime;

        enum EventStreamId
        {
            Start = 0,
            End = 1,
        }

        enum StatePosition
        {
            PreStart = 0,

            PreEnd,

            Finished,
        }

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IActionStateObservable>(this);
            owner.RegisterInterface<IActionStateController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            // キャンセル設定
            _onEnterState.Subscribe(_ => _activeCancelEvents.Clear());
            GetEventStream<ActionEventCancel>(EventStreamId.Start).Subscribe(c => _activeCancelEvents.Add(c));
            GetEventStream<ActionEventCancel>(EventStreamId.End).Subscribe(c => _activeCancelEvents.Remove(c));

            // フレーム処理
            if (Owner.QueryInterface(out IUpdateObservable updateObservable) == true && Owner.QueryInterface(out IDeltaTime deltaTime) == true)
            {
                updateObservable.OnUpdate((int)UPDATE_ORDER.ACTION_TRANSITION)
                    .Subscribe(_ => OnUpdate(deltaTime))
                    .AddTo(Disposables);
            }
        }

        void OnUpdate(IDeltaTime deltaTime)
        {
            ActionState nextState = _nextState;
            _nextState = null;

            // 初期ステート
            if (_currentState.Value == null && _initialState != null)
            {
                nextState = _initialState;
            }

            // コマンド処理
            if (nextState == null && _requestCommand.IsEmpty == false)
            {
                var cancelEvent = _activeCancelEvents.Find(x => x.CommandId == _requestCommand.Id);
                if (cancelEvent != null)
                {
                    _executedCommand.Value = _requestCommand;
                    if (cancelEvent.HasStateType == true)
                    {
                        nextState = _actionStates.FirstOrDefault(x => x.Type == cancelEvent.StateType);
                    }
                }
                _requestCommand = Command.Empty;
            }

            // 遷移
            if (nextState != null)
            {
                StartNewState(nextState);
            }

            // 時間経過
            _currentStateTime += deltaTime.DeltaTime;

            // 各イベントの時間経過
            if (_currentState.Value != null)
            {
                var events = _currentState.Value.Events;
                for (int evIndex = 0; evIndex < events.Count; ++evIndex)
                {
                    var ev = events[evIndex];
                    var position = _eventPositions[evIndex];
                    if (position == StatePosition.PreStart)
                    {
                        if (ev.Start <= _currentStateTime)
                        {
                            position = StatePosition.PreEnd;
                            ev.OnStart(this);
                        }
                    }
                    if (position == StatePosition.PreEnd)
                    {
                        if (ev.End <= _currentStateTime)
                        {
                            position = StatePosition.Finished;
                            ev.OnEnd(this);
                        }
                    }
                    _eventPositions[evIndex] = position;
                }
            }
        }

        void StartNewState(ActionState newActionState)
        {
            // 前のステートからの脱出
            if (_currentState.Value != null)
            {
                // End できていないevent処理
                var events = _currentState.Value.Events;
                for (int evIndex = 0; evIndex < events.Count; ++evIndex)
                {
                    if (_eventPositions[evIndex] == StatePosition.PreEnd)
                    {
                        events[evIndex].OnEnd(this);
                    }
                }

                // State 脱出通知
                _onExitState.OnNext(_currentState.Value);
            }

            // 新しいステートへ入る
            _currentState.Value = newActionState;
            _currentStateTime = 0f;

            if (newActionState != null)
            {
                // ActionEvent の状態初期化
                for (int evIndex = 0; evIndex < newActionState.Events.Count; ++evIndex)
                {
                    _eventPositions[evIndex] = StatePosition.PreStart;
                }

                // State 突入通知
                _onEnterState.OnNext(newActionState);

                _stateHistory.Add(newActionState);
            }
        }

        IObservable<ActionState> IActionStateObservable.OnEnterState => _onEnterState;

        IObservable<ActionState> IActionStateObservable.OnExitState => _onExitState;

        IObservable<T> IActionStateObservable.OnStart<T>()
        {
            return GetEventStream<T>(EventStreamId.Start);
        }

        IObservable<T> IActionStateObservable.OnEnd<T>()
        {
            return GetEventStream<T>(EventStreamId.End);
        }

        void IActionEventNotifier.OnStart<T>(T stateEvent)
        {
            GetEventStream<T>(EventStreamId.Start).OnNext(stateEvent);
        }

        void IActionEventNotifier.OnEnd<T>(T stateEvent)
        {
            GetEventStream<T>(EventStreamId.End).OnNext(stateEvent);
        }

        void IActionStateController.AddState(ActionState state)
        {
            _actionStates.Add(state);
        }

        void IActionStateController.SetInitialState(ActionState state)
        {
            Assert.IsTrue(_actionStates.Find(x => x == state) != null);
            _initialState = state;
        }

        void IActionStateController.Execute(Command command)
        {
            _requestCommand = command;
        }

        void IActionStateController.ExecuteTransition(ActionStateType stateType)
        {
            InternalExecuteTransition(stateType);
        }

        void InternalExecuteTransition(ActionStateType stateType)
        {
            var newState = _actionStates.FirstOrDefault(x => x.Type == stateType);
            if (newState != null)
            {
                _nextState = newState;
            }
        }

        ISubject<T> GetEventStream<T>(EventStreamId streamId)
        {
            var type = typeof(T);
            if (_eventStreamPool.TryGetValue(type, out object[] streams) == false)
            {
                _eventStreamPool[type] = streams = new object[] { new Subject<T>(), new Subject<T>(), };
            }
            return streams[(int)streamId] as ISubject<T>;
        }
    }
}
