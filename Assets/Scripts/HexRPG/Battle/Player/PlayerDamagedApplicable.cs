using UnityEngine;
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

            base.InternalInit();

            //TODO: �e�X�g�R�[�h
            DamagedTest();
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
                            Damage = 10,
                            HitType = HitType.WEAK
                        };
                        if (_memberObservable.CurMember.Value.DieObservable.IsDead.Value == false)
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
