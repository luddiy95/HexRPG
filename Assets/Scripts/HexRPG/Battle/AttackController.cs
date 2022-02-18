using System;
using System.Collections.Generic;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface IAttackObservable
    {
        IObservable<HitData> OnAttackHit { get; }
    }

    public interface IAttackApplicator
    {
        IAttackSetting CurrentSetting { get; }

        ICharacterComponentCollection AttackOrigin { get; }

        bool TryMarkAsHit(ICharacterComponentCollection damagedObject);

        void NotifyAttackHit(HitData hitData);
    }

    public interface IAttackController
    {
        void StartAttack(List<Hex> attackRange, IAttackSetting setting, ICharacterComponentCollection attackOrigin);

        void FinishAttack();
    }

    public class AttackController : IAttackApplicator, IAttackController, IAttackObservable
    {
        List<Hex> _curAttackRange = new List<Hex>();

        IAttackSetting IAttackApplicator.CurrentSetting => _currentSetting;
        IAttackSetting _currentSetting = null;

        ICharacterComponentCollection IAttackApplicator.AttackOrigin => _attackOrigin;
        ICharacterComponentCollection _attackOrigin;

        IObservable<HitData> IAttackObservable.OnAttackHit => _onAttackHit;
        readonly ISubject<HitData> _onAttackHit = new Subject<HitData>();

        private List<ICharacterComponentCollection> _hitObjects = new List<ICharacterComponentCollection>();

        void IAttackController.StartAttack(List<Hex> attackRange, IAttackSetting setting, ICharacterComponentCollection attackOrigin)
        {
            _curAttackRange = attackRange;
            _currentSetting = setting;
            _attackOrigin = attackOrigin;
            _curAttackRange.ForEach(hex =>
            {
                hex.AddAttackApplicator(this);
            });
            _hitObjects.Clear();
        }

        void IAttackController.FinishAttack()
        {
            _curAttackRange.ForEach(hex =>
            {
                hex.RemoveAttackApplicator(this);
            });
            _currentSetting = null;
        }

        bool IAttackApplicator.TryMarkAsHit(ICharacterComponentCollection owner)
        {
            if (_currentSetting == null)
            {
                return false;
            }
            if (_hitObjects.Contains(owner) == true)
            {
                return false;
            }
            _hitObjects.Add(owner);
            return true;
        }

        void IAttackApplicator.NotifyAttackHit(HitData hitData)
        {
            _onAttackHit.OnNext(hitData);
        }
    }
}