using System.Collections.Generic;
using System;
using System.Linq;
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
        void StartAttack(IAttackSetting setting);

        void FinishAttack();
    }

    public interface IAttackReservation
    {
        ICharacterComponentCollection ReservationOrigin { get; }
    }

    public interface IAttackReserve
    {
        void StartAttackReservation(in List<Hex> reservationRange);
        void FinishAttackReservation();
    }

    public class AttackController : IAttackApplicator, IAttackController, IAttackObservable, IAttackReservation, IAttackReserve
    {
        List<Hex> _curReservationRange = new List<Hex>();

        IAttackSetting IAttackApplicator.CurrentSetting => _currentSetting;
        IAttackSetting _currentSetting = null;

        ICharacterComponentCollection IAttackReservation.ReservationOrigin => _owner;
        ICharacterComponentCollection IAttackApplicator.AttackOrigin => _owner;

        ICharacterComponentCollection _owner;

        IObservable<HitData> IAttackObservable.OnAttackHit => _onAttackHit;
        readonly ISubject<HitData> _onAttackHit = new Subject<HitData>();

        readonly List<ICharacterComponentCollection> _hitObjects = new List<ICharacterComponentCollection>(32);

        public AttackController(ICharacterComponentCollection owner)
        {
            _owner = owner;
        }

        void IAttackReserve.StartAttackReservation(in List<Hex> reservationRange)
        {
            _curReservationRange = reservationRange;
            foreach (var hex in _curReservationRange) hex.AddAttackReservation(this);
        }

        void IAttackReserve.FinishAttackReservation()
        {
            foreach (var hex in _curReservationRange) hex.RemoveAttackReservation(this);
        }

        void IAttackController.StartAttack(IAttackSetting setting)
        {
            _currentSetting = setting;
            if (_currentSetting is ICombatAttackSetting combatAttackSetting)
            {
                combatAttackSetting.AttackColliders.ForEach(collider => collider.gameObject.SetActive(true));
            }
            if (_currentSetting is ISkillAttackSetting skillAttackSetting)
            {
                skillAttackSetting.AttackRange.ForEach(hex => hex.AddAttackApplicator(this));
            }
            _hitObjects.Clear();
        }

        void IAttackController.FinishAttack()
        {
            if(_currentSetting is ICombatAttackSetting combatAttackSetting)
            {
                combatAttackSetting.AttackColliders.ForEach(collider => collider.gameObject.SetActive(false));
            }
            if(_currentSetting is ISkillAttackSetting skillAttackSetting)
            {
                skillAttackSetting.AttackRange.ForEach(hex => hex.RemoveAttackApplicator(this));
            }
            _currentSetting = null;
        }

        bool IAttackApplicator.TryMarkAsHit(ICharacterComponentCollection damagedOwner)
        {
            if (_currentSetting == null)
            {
                return false;
            }
            if (_hitObjects.Contains(damagedOwner) == true)
            {
                return false;
            }
            _hitObjects.Add(damagedOwner);
            return true;
        }

        void IAttackApplicator.NotifyAttackHit(HitData hitData)
        {
            _onAttackHit.OnNext(hitData);
        }
    }
}