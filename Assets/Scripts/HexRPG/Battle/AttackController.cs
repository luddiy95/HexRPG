using System;
using System.Collections.Generic;
using UniRx;

namespace HexRPG.Battle
{
    using Stage;

    public interface IAttackObservable : IFeature
    {
        IObservable<HitData> OnAttackHit { get; }
    }

    public interface IAttackApplicator : IFeature
    {
        IAttackSetting CurrentSetting { get; }

        ICustomComponentCollection AttackOrigin { get; }

        bool TryMarkAsHit(ICustomComponentCollection damagedObject);

        void NotifyAttackHit(HitData hitData);
    }

    public interface IAttackController : IFeature
    {
        void StartAttack(List<Hex> attackRange, IAttackSetting setting, ICustomComponentCollection attackOrigin);

        void FinishAttack();
    }

    public class AttackController : AbstractCustomComponentBehaviour, IAttackApplicator, IAttackController, IAttackObservable
    {
        List<Hex> _curAttackRange = new List<Hex>();

        IAttackSetting IAttackApplicator.CurrentSetting => _currentSetting;
        IAttackSetting _currentSetting = null;

        ICustomComponentCollection IAttackApplicator.AttackOrigin => _attackOrigin;
        ICustomComponentCollection _attackOrigin;

        IObservable<HitData> IAttackObservable.OnAttackHit => _onAttackHit;
        readonly ISubject<HitData> _onAttackHit = new Subject<HitData>();

        private List<ICustomComponentCollection> _hitObjects = new List<ICustomComponentCollection>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IAttackController>(this);
            owner.RegisterInterface<IAttackObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        void IAttackController.StartAttack(List<Hex> attackRange, IAttackSetting setting, ICustomComponentCollection attackOrigin)
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

        bool IAttackApplicator.TryMarkAsHit(ICustomComponentCollection owner)
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