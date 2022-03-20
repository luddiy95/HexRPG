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
        void StartAttackReservation(List<Hex> reservationRange, ICharacterComponentCollection ReservationOrigin);
        void FinishAttackReservation();
    }

    public class AttackController : IAttackApplicator, IAttackController, IAttackObservable, IAttackReservation, IAttackReserve
    {
        List<Hex> _curReservationRange = new List<Hex>();
        List<Hex> _curAttackRange = new List<Hex>();

        IAttackSetting IAttackApplicator.CurrentSetting => _currentSetting;
        IAttackSetting _currentSetting = null;

        ICharacterComponentCollection IAttackReservation.ReservationOrigin => _reservationOrigin;
        ICharacterComponentCollection _reservationOrigin;

        ICharacterComponentCollection IAttackApplicator.AttackOrigin => _attackOrigin;
        ICharacterComponentCollection _attackOrigin;

        IObservable<HitData> IAttackObservable.OnAttackHit => _onAttackHit;
        readonly ISubject<HitData> _onAttackHit = new Subject<HitData>();

        private List<ICharacterComponentCollection> _hitObjects = new List<ICharacterComponentCollection>();

        void IAttackReserve.StartAttackReservation(List<Hex> reservationRange, ICharacterComponentCollection reservationOrigin)
        {
            _curReservationRange = reservationRange;
            _reservationOrigin = reservationOrigin;
            _curReservationRange.ForEach(hex => hex.AddAttackReservation(this));
        }

        void IAttackReserve.FinishAttackReservation()
        {
            _curReservationRange.ForEach(hex => hex.RemoveAttackReservation(this));
        }

        void IAttackController.StartAttack(IAttackSetting setting, ICharacterComponentCollection attackOrigin)
        {
            _currentSetting = setting;
            _attackOrigin = attackOrigin;
            if (setting is ICombatAttackSetting combatAttackSetting)
            {

            }else if (setting is ISkillAttackSetting skillAttackSetting)
            {
                _curAttackRange = skillAttackSetting.AttackRange;
                _curAttackRange.ForEach(hex =>
                {
                    hex.AddAttackApplicator(this);
                });
            }
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