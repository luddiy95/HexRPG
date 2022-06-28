using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    public class EnemyDamagedApplicable : AbstractDamagedApplicable
    {
        IColliderController _colliderController;
        IDieObservable _dieObservable;

        [Inject]
        public void Construct(
            ICharacterComponentCollection owner,
            IUpdateObservable updateObservable,
            IColliderController colliderController,
            IDieObservable dieObservable
         )
        {
            _damagedOwner = owner;
            _updateObservable = updateObservable;
            _colliderController = colliderController;
            _dieObservable = dieObservable;
        }

        protected override void InternalInit()
        {
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

            base.InternalInit();

            //TODO: テストコード
            DamagedTest();
        }

        protected override void InternalDoHit(IAttackApplicator attackApplicator)
        {
            // 死亡中はHitしない
            if (_dieObservable.IsDead.Value) return;

            if (attackApplicator.AttackOrigin is IEnemyComponentCollection) return;

            base.InternalDoHit(attackApplicator);
        }

        void DamagedTest()
        {
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        var hitData = new HitData
                        {
                            DamagedObject = _damagedOwner,
                            Damage = 0,
                            HitType = HitType.WEAK
                        };
                        if (_dieObservable.IsDead.Value == false)
                        {
                            _onHit.OnNext(hitData);
                            _damagedOwner.Health.Update(-hitData.Damage);
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        var hitData = new HitData
                        {
                            DamagedObject = _damagedOwner,
                            Damage = 300,
                            HitType = HitType.WEAK
                        };
                        if (_dieObservable.IsDead.Value == false)
                        {
                            _onHit.OnNext(hitData);
                            _damagedOwner.Health.Update(-hitData.Damage);
                        }
                    }
                })
                .AddTo(_disposables);
        }
    }
}
