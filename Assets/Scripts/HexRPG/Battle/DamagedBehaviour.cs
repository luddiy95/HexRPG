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

    public class DamagedBehaviour : MonoBehaviour, IDamageApplicable, IInitializable
    {
        ICharacterComponentCollection _damagedOwner;
        ITransformController _transformController;
        IUpdateObservable _updateObservable;

        public IObservable<HitData> OnHit => _onHit;
        readonly ISubject<HitData> _onHit = new Subject<HitData>();

        private readonly List<AttackCollider> _hitAttacks = new List<AttackCollider>();

        bool _isEnemy = false;

        [Inject]
        public void Construct(
            ICharacterComponentCollection owner, 
            ITransformController transformController, 
            IUpdateObservable updateObservable
         )
        {
            _damagedOwner = owner; 
            _transformController = transformController; 
            _updateObservable = updateObservable; 
        }

        void IInitializable.Initialize()
        {
            _isEnemy = (_damagedOwner is IEnemyComponentCollection);

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
                .AddTo(this);

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
                        _onHit.OnNext(new HitData());
                        if (_damagedOwner is IPlayerComponentCollection playerOwner) playerOwner.MemberObservable.CurMember.Value.Health.Update(-10);
                        //if (_damagedOwner is IEnemyComponentCollection enemyOwner) enemyOwner.Health.Update(-1000000000);
                    }
                })
                .AddTo(this);
        }

        void DoHit(IAttackApplicator attackApplicator)
        {
            if ((attackApplicator.AttackOrigin is IEnemyComponentCollection) == _isEnemy) return;

            // ヒット済みマーク失敗＝すでにヒットしてる
            if (attackApplicator.TryMarkAsHit(_damagedOwner) == false)
            {
                return;
            }

            var hitData = new HitData
            {
                AttackApplicator = attackApplicator,
                DamagedObject = _damagedOwner,
                Damage = attackApplicator.CurrentSetting.Power
            };

            // コールバック
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);

            //TODO: 【ここから】IPlayerComponentCollectionじゃなくてIMemberComponentCollectionじゃない？(現在DamagedBehaviourがPlayerにアタッチされているが各Memberじゃない？)
            //TODO: EnemyはCombatが存在しないからAttackEnableはHex経由だけのためColliderがいらない->Playerにアタッチされている？
            if (_damagedOwner is IPlayerComponentCollection playerOwner) playerOwner.MemberObservable.CurMember.Value.Health.Update(-hitData.Damage);
            if (_damagedOwner is IEnemyComponentCollection enemyOwner) enemyOwner.Health.Update(-hitData.Damage);
        }
    }

    public struct HitData
    {
        public IAttackApplicator AttackApplicator { get; set; }
        public ICharacterComponentCollection DamagedObject { get; set; }
        public int Damage { get; set; }
    }

}