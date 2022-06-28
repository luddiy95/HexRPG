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
            // �Փ˃L���[�C���O
            _colliderController.Collider.OnTriggerEnterAsObservable()
                .Subscribe(x =>
                {
                    // �U�����ǂ���
                    if (x.transform.TryGetComponent<AttackCollider>(out var attackCollider) == false)
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
                })
                .AddTo(_disposables);

            base.InternalInit();

            //TODO: �e�X�g�R�[�h
            DamagedTest();
        }

        protected override void InternalDoHit(IAttackApplicator attackApplicator)
        {
            // ���S����Hit���Ȃ�
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
