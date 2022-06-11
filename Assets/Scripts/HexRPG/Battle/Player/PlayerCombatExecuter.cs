using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Combat;

    public class PlayerCombatController : ICombatController, ICombatObservable, IDisposable
    {
        IUpdateObservable _updateObservable;
        ILocomotionController _locomotionController;
        IMemberObservable _memberObservable;
        IAttackController _attackController;

        IObservable<CombatAttackSetting> ICombatObservable.OnCombatAttackEnable => null;
        IObservable<Unit> ICombatObservable.OnCombatAttackDisable => null;
        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection _runningCombat;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerCombatController(
            IUpdateObservable updateObservable,
            ILocomotionController locomotionController,
            IMemberObservable memberObservable,
            IAttackController attackController
        )
        {
            _updateObservable = updateObservable;
            _locomotionController = locomotionController;
            _memberObservable = memberObservable;
            _attackController = attackController;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            //TODO: Playerを仲介する必要があるのか(Skillは必要だけど。。)

            var combatController = _memberObservable.CurMember.Value.CombatController;
            if (_runningCombat != null)
            {
                // コンボ入力
                combatController.Combat();
            }
            else
            {
                _runningCombat = combatController.Combat();

                _disposables.Clear();
                _runningCombat.CombatObservable.OnCombatAttackEnable
                    .Subscribe(attackSetting => _attackController.StartAttack(attackSetting))
                    .AddTo(_disposables);
                _runningCombat.CombatObservable.OnCombatAttackDisable
                    .Subscribe(_ => _attackController.FinishAttack())
                    .AddTo(_disposables);
                // 終了処理
                _runningCombat.CombatObservable.OnFinishCombat
                    .Subscribe(_ =>
                    {
                        _locomotionController.Stop();
                        _runningCombat = null;
                        _disposables.Clear();

                        _onFinishCombat.OnNext(Unit.Default);
                    })
                    .AddTo(_disposables);
                // Velocity更新
                _updateObservable
                    .OnUpdate((int)UPDATE_ORDER.INPUT)
                    .Subscribe(_ =>
                    {
                        var velocity = _runningCombat.Combat.Velocity;
                        _locomotionController.SetSpeed(velocity, velocity.magnitude);
                    })
                    .AddTo(_disposables);
            }

            return _runningCombat;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
