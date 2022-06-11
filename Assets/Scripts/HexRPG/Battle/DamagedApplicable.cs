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

    public class DamagedApplicable : IDamageApplicable, IInitializable, IDisposable
    {
        ICharacterComponentCollection _damagedOwner;
        ITransformController _transformController;
        IColliderController _colliderController;
        IUpdateObservable _updateObservable;

        public IObservable<HitData> OnHit => _onHit;
        readonly ISubject<HitData> _onHit = new Subject<HitData>();

        private readonly List<AttackCollider> _hitAttacks = new List<AttackCollider>();

        bool _isEnemy = false;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            ICharacterComponentCollection owner, 
            ITransformController transformController, 
            IColliderController colliderController,
            IUpdateObservable updateObservable
         )
        {
            _damagedOwner = owner; 
            _transformController = transformController;
            _colliderController = colliderController;
            _updateObservable = updateObservable; 
        }

        void IInitializable.Initialize()
        {
            _isEnemy = (_damagedOwner is IEnemyComponentCollection);

            // 衝突キューイング
            _colliderController.Collider.OnTriggerEnterAsObservable()
                .Subscribe(x =>
                {
                    // 攻撃かどうか
                    if (x.transform.TryGetComponent<AttackCollider>(out var attackCollider) == false)
                    {
                        return;
                    }
                    // 自分の攻撃かどうか
                    if (attackCollider.AttackApplicator.AttackOrigin == _damagedOwner)
                    {
                        return;
                    }
                    // すでにヒット処理済みかどうか
                    if (_hitAttacks.Contains(attackCollider) == true)
                    {
                        return;
                    }
                    // 死んでないかどうか
                    _hitAttacks.Add(attackCollider);
                })
                .AddTo(_disposables);

            // ダメージ処理
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    // Combat攻撃
                    foreach(var attackCollider in _hitAttacks)
                    {
                        DoHit(attackCollider.AttackApplicator);
                    }
                    _hitAttacks.Clear();

                    // Skill攻撃
                    var landedHex = _transformController.GetLandedHex();
                    foreach (var attackApplicator in landedHex.AttackApplicatorList)
                    {
                        if (attackApplicator.AttackOrigin == _damagedOwner) continue;
                        DoHit(attackApplicator);
                    }

                    //TODO: テストコード
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        var hitData = new HitData
                        {
                            DamagedObject = _damagedOwner,
                            Damage = 10,
                            HitType = HitType.WEAK
                        };
                        if (_damagedOwner is IPlayerComponentCollection playerOwner && 
                                playerOwner.MemberObservable.CurMember.Value.DieObservable.IsDead.Value == false)
                        {
                            _onHit.OnNext(hitData);
                            playerOwner.Health.Update(-hitData.Damage);
                        }
                        /*
                        if (_damagedOwner is IEnemyComponentCollection enemyOwner && enemyOwner.DieObservable.IsDead.Value == false)
                        {
                            _onHit.OnNext(hitData);
                            enemyOwner.Health.Update(-hitData.Damage);
                        }
                        */
                    }
                })
                .AddTo(_disposables);
        }

        void DoHit(IAttackApplicator attackApplicator)
        {
            // 死亡中はHitしない
            if (_isEnemy)
            {
                if (_damagedOwner.DieObservable.IsDead.Value) return;
            }
            else if (_damagedOwner is IPlayerComponentCollection playerOwner)
            {
                if (playerOwner.MemberObservable.CurMember.Value.DieObservable.IsDead.Value) return;
            }

            if ((attackApplicator.AttackOrigin is IEnemyComponentCollection) == _isEnemy) return;

            // ヒット済みマーク失敗＝すでにヒットしてる
            if (attackApplicator.TryMarkAsHit(_damagedOwner) == false) return;

            //TODO: Criticalかどうか(CriticalとWeak/Resistは両立しない)
            var hitType = HitType.NORMAL;
            if(attackApplicator.CurrentSetting is ISkillAttackSetting skillAttackSetting)
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

            //! EnemyはCombatが存在しないからAttackEnableはHex経由だけのためColliderがいらない->Playerにアタッチされている
            _damagedOwner.Health.Update(-hitData.Damage);
        }

        void IDisposable.Dispose()
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