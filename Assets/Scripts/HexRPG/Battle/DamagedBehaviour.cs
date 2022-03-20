using System.Collections.Generic;
using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle
{
    public interface IDamageApplicable
    {
        IObservable<HitData> OnHit { get; }
    }

    public class DamagedBehaviour : MonoBehaviour, IDamageApplicable, IInitializable
    {
        ICharacterComponentCollection _owner;
        ITransformController _transformController;
        IUpdateObservable _updateObservable;
        IHealth _health;

        public IObservable<HitData> OnHit => _onHit;
        readonly ISubject<HitData> _onHit = new Subject<HitData>();

        private readonly List<AttackCollider> _hitAttacks = new List<AttackCollider>();

        [Inject]
        public void Construct(
            ICharacterComponentCollection owner, 
            ITransformController transformController, 
            IUpdateObservable updateObservable, 
            IHealth health)
        {
            _owner = owner; 
            _transformController = transformController; 
            _updateObservable = updateObservable; 
            _health = health;
        }

        void IInitializable.Initialize()
        {
            // 衝突キューイング
            this.OnTriggerEnterAsObservable()
                .Subscribe(x =>
                {
                    // 攻撃かどうか
                    if (x.transform.TryGetComponent<AttackCollider>(out var attackCollider) == false)
                    {
                        return;
                    }
                    // 自分の攻撃かどうか
                    if (attackCollider.AttackApplicator.AttackOrigin == _owner)
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
                .AddTo(this);

            // ダメージ処理
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    //TODO: Enemyの場合とPlayerの場合でダメージを受けるattackOriginが違う->isPlayerなどのフラグ

                    // Combat攻撃
                    foreach(var attackCollider in _hitAttacks)
                    {
                        DoHit(attackCollider.AttackApplicator);
                    }

                    // Skill攻撃
                    var landedHex = _transformController.GetLandedHex();
                    foreach (var attackApplicator in landedHex.AttackApplicatorList)
                    {
                        DoHit(attackApplicator);
                    }
                })
                .AddTo(this);
        }

        void DoHit(IAttackApplicator attackApplicator)
        {
            // ヒット済みマーク失敗＝すでにヒットしてる
            if (attackApplicator.TryMarkAsHit(_owner) == false)
            {
                return;
            }

            var hitData = new HitData
            {
                AttackApplicator = attackApplicator,
                DamagedObject = _owner,
                Damage = attackApplicator.CurrentSetting.Power
        };

            // health を減らす
            _health.Update(-hitData.Damage);

            // コールバック
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);
        }
    }

    public struct HitData
    {
        public IAttackApplicator AttackApplicator { get; set; }
        public ICharacterComponentCollection DamagedObject { get; set; }
        public int Damage { get; set; }
    }

}