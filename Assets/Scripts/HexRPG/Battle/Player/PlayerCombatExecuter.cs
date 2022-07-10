using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Combat;

    public class PlayerCombatExecuter : ICombatController, ICombatObservable, IDisposable
    {
        IUpdateObservable _updateObservable;
        ILocomotionController _locomotionController;
        IMemberObservable _memberObservable;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection _runningCombat;

        CompositeDisposable _disposables = new CompositeDisposable();

        public PlayerCombatExecuter(
            IUpdateObservable updateObservable,
            ILocomotionController locomotionController,
            IMemberObservable memberObservable
        )
        {
            _updateObservable = updateObservable;
            _locomotionController = locomotionController;
            _memberObservable = memberObservable;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            //TODO: Player�𒇉��K�v������̂�(Skill�͕K�v�����ǁB�B)

            var combatController = _memberObservable.CurMember.Value.CombatController;
            if (_runningCombat != null)
            {
                // �R���{����
                combatController.Combat();
            }
            else
            {
                _runningCombat = combatController.Combat();

                _disposables.Clear();
                // �I������
                _runningCombat.CombatObservable.OnFinishCombat
                    .Subscribe(_ =>
                    {
                        _locomotionController.Stop();
                        _runningCombat = null;

                        _onFinishCombat.OnNext(Unit.Default);

                        _disposables.Clear();
                    })
                    .AddTo(_disposables);
                // Velocity�X�V
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
