using System;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle.Player
{
    public class PlayerDamagedApplicable : AbstractDamagedApplicable
    {
        IMemberObservable _memberObservable;

        IDisposable _memberChangeDisposable;

        [Inject]
        public void Construct(
            ICharacterComponentCollection owner,
            IUpdateObservable updateObservable,
            IMemberObservable memberObservable
         )
        {
            _damagedOwner = owner;
            _updateObservable = updateObservable;
            _memberObservable = memberObservable;
        }

        protected override void InternalInit()
        {
            _memberObservable.CurMember
                .Skip(1)
                .Subscribe(memberOwner =>
                {
                    _memberChangeDisposable?.Dispose();
                    _memberChangeDisposable = memberOwner.ColliderController.Collider.OnTriggerEnterAsObservable()
                        .Subscribe(collider =>
                        {
                            // 攻撃かどうか
                            if (collider.transform.TryGetComponent<AttackCollider>(out var attackCollider) == false)
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
                        });
                })
                .AddTo(_disposables);

            base.InternalInit();

            //TODO: テストコード
            //AllHitTest();
        }

        protected override void InternalDoHit(IAttackApplicator attackApplicator)
        {
            // 死亡中はHitしない
            if (_memberObservable.CurMember.Value.DieObservable.IsDead.Value) return;

            if (attackApplicator.AttackOrigin is IPlayerComponentCollection) return;

            base.InternalDoHit(attackApplicator);
        }

        protected override void InternalDispose()
        {
            _memberChangeDisposable?.Dispose();
            base.InternalDispose();
        }

        protected override void OnHitTest(int? damage = null)
        {
            var hitData = new HitData
            {
                DamagedOwner = _damagedOwner,
                Damage = damage ?? 0,
                HitType = HitType.WEAK
            };
            if (_memberObservable.CurMember.Value.DieObservable.IsDead.Value == false)
            {
                _onHit.OnNext(hitData);
                _damagedOwner.Health.Update(-hitData.Damage);
            }
        }
    }
}
