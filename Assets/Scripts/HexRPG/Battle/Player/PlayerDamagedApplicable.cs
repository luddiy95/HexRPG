using System;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle.Player
{
    public class PlayerDamagedApplicable : AbstractDamagedApplicable
    {
        BattleData _battleData;
        IMemberObservable _memberObservable;

        IDisposable _memberChangeDisposable;

        [Inject]
        public void Construct(
            BattleData battleData,
            ICharacterComponentCollection owner,
            IUpdateObservable updateObservable,
            IMemberObservable memberObservable
         )
        {
            _battleData = battleData;
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
                            // �U�����ǂ���
                            if (collider.transform.TryGetComponent<AttackCollider>(out var attackCollider) == false)
                            {
                                return;
                            }
                            // �����̍U�����ǂ���
                            if (attackCollider.AttackApplicator.AttackOrigin == _damagedOwner)
                            {
                                return;
                            }
                            // ���łɃq�b�g�����ς݂��ǂ���
                            if (_hitAttacks.Contains(attackCollider) == true)
                            {
                                return;
                            }
                            // ����łȂ����ǂ���
                            _hitAttacks.Add(attackCollider);
                        });
                })
                .AddTo(_disposables);

            _onHit
                .Subscribe(hitData =>
                {
                    if (_battleData.hitTypeSkillpointMap.Table.TryGetValue(hitData.HitType, out int getAmount))
                    {
                        _memberObservable.CurMember.Value.SkillPoint.Update(getAmount);
                    }
                })
                .AddTo(_disposables);

            base.InternalInit();

            //TODO: �e�X�g�R�[�h
            //AllHitTest();
        }

        protected override void InternalDoHit(IAttackApplicator attackApplicator)
        {
            // ���S����Hit���Ȃ�
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
