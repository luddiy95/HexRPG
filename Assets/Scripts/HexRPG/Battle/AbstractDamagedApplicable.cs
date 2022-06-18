using System.Collections.Generic;
using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle
{
    using Player;
    using Enemy;

    public interface IDamageApplicable
    {
        IObservable<HitData> OnHit { get; }
    }

    public abstract class AbstractDamagedApplicable : IDamageApplicable, IInitializable, IDisposable
    {
        protected ICharacterComponentCollection _damagedOwner;
        IColliderController _colliderController;
        protected IUpdateObservable _updateObservable;

        public IObservable<HitData> OnHit => _onHit;
        protected readonly ISubject<HitData> _onHit = new Subject<HitData>();

        protected readonly List<AttackCollider> _hitAttacks = new List<AttackCollider>();

        protected CompositeDisposable _disposables = new CompositeDisposable();

        bool _isEnemy = false;

        void IInitializable.Initialize()
        {
            InternalInit();
        }

        protected virtual void InternalInit()
        {
            // ダメージ処理
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    // Combat攻撃
                    foreach (var attackCollider in _hitAttacks)
                    {
                        DoHit(attackCollider.AttackApplicator);
                    }
                    _hitAttacks.Clear();

                    // Skill攻撃
                    var landedHex = _damagedOwner.TransformController.GetLandedHex();
                    foreach (var attackApplicator in landedHex.AttackApplicatorList)
                    {
                        if (attackApplicator.AttackOrigin == _damagedOwner) continue;
                        DoHit(attackApplicator);
                    }
                })
                .AddTo(_disposables);
        }

        void DoHit(IAttackApplicator attackApplicator)
        {
            InternalDoHit(attackApplicator);
        }

        protected virtual void InternalDoHit(IAttackApplicator attackApplicator)
        {
            // ヒット済みマーク失敗＝すでにヒットしてる
            if (attackApplicator.TryMarkAsHit(_damagedOwner) == false) return;

            //TODO: Criticalかどうか(CriticalとWeak/Resistは両立しない)
            var hitType = HitType.NORMAL;
            if (attackApplicator.CurrentSetting is ISkillAttackSetting skillAttackSetting)
            {
                var attackAttribute = skillAttackSetting.Attribute;
                var damagedAttribute = _damagedOwner.ProfileSetting.Attribute;
                if (attackAttribute.IsWeakCompatibity(damagedAttribute)) hitType = HitType.RESIST;
                else if (damagedAttribute.IsWeakCompatibity(attackAttribute)) hitType = HitType.WEAK;
            }

            var damage = attackApplicator.CurrentSetting.Power;
            switch (hitType)
            {
                case HitType.WEAK:
                case HitType.CRITICAL: damage *= 2; break;
                case HitType.RESIST: damage /= 2; break;
            }

            var hitData = new HitData
            {
                AttackApplicator = attackApplicator,
                DamagedObject = _damagedOwner,
                Damage = damage,
                HitType = hitType
            };

            // コールバック
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);

            _damagedOwner.Health.Update(-hitData.Damage);
        }

        void IDisposable.Dispose()
        {
            InternalDispose();
        }

        protected virtual void InternalDispose()
        {
            _disposables.Dispose();
        }
    }

    public enum HitType
    {
        NORMAL,

        WEAK,
        RESIST,
        CRITICAL
    }

    public struct HitData
    {
        public IAttackApplicator AttackApplicator { get; set; }
        public ICharacterComponentCollection DamagedObject { get; set; }
        public int Damage { get; set; }
        public HitType HitType { get; set; }
    }

}