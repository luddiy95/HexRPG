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
        void StartAttack(IAttackSetting setting, ICharacterComponentCollection attackOrigin);

        void FinishAttack();
    }

    public interface IAttackReservation
    {
        ICharacterComponentCollection ReservationOrigin { get; }
    }

    public interface IAttackReserve
    {
        void StartAttackReservation(Hex[] reservationRange, ICharacterComponentCollection ReservationOrigin);
        void FinishAttackReservation();
    }

    public class AttackController : IAttackApplicator, IAttackController, IAttackObservable, IAttackReservation, IAttackReserve
    {
        Hex[] _curReservationRange = new Hex[0];

        IAttackSetting IAttackApplicator.CurrentSetting => _currentSetting;
        IAttackSetting _currentSetting = null;

        ICharacterComponentCollection IAttackReservation.ReservationOrigin => _reservationOrigin;
        ICharacterComponentCollection _reservationOrigin;

        ICharacterComponentCollection IAttackApplicator.AttackOrigin => _attackOrigin;
        ICharacterComponentCollection _attackOrigin;

        IObservable<HitData> IAttackObservable.OnAttackHit => _onAttackHit;
        readonly ISubject<HitData> _onAttackHit = new Subject<HitData>();

        private List<ICharacterComponentCollection> _hitObjects = new List<ICharacterComponentCollection>();

        void IAttackReserve.StartAttackReservation(Hex[] reservationRange, ICharacterComponentCollection reservationOrigin)
        {
            _curReservationRange = reservationRange;
            _reservationOrigin = reservationOrigin;
            Array.ForEach(_curReservationRange, hex => hex.AddAttackReservation(this));
        }

        void IAttackReserve.FinishAttackReservation()
        {
            Array.ForEach(_curReservationRange, hex => hex.RemoveAttackReservation(this));
        }

        void IAttackController.StartAttack(IAttackSetting setting, ICharacterComponentCollection attackOrigin)
        {
            _currentSetting = setting;
            _attackOrigin = attackOrigin;
            if (setting is ICombatAttackSetting combatAttackSetting)
            {
                combatAttackSetting.AttackColliders.ForEach(attackCollider => attackCollider.gameObject.SetActive(true));
            }
            if (setting is ISkillAttackSetting skillAttackSetting)
            {
                Array.ForEach(skillAttackSetting.AttackRange, hex => hex.AddAttackApplicator(this));
            }
            _hitObjects.Clear();
        }

        void IAttackController.FinishAttack()
        {
            if(_currentSetting is ICombatAttackSetting combatAttackSetting)
            {
                combatAttackSetting.AttackColliders.ForEach(attackCollider => attackCollider.gameObject.SetActive(false));
            }
            if(_currentSetting is ISkillAttackSetting skillAttackSetting)
            {
                Array.ForEach(skillAttackSetting.AttackRange, hex => hex.RemoveAttackApplicator(this));
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