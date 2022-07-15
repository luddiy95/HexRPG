using UniRx;
using UniRx.Triggers;
using Zenject;

namespace HexRPG.Battle.Stage.Tower
{
    using Player;
    using Enemy;

    public class TowerDamagedApplicable : AbstractDamagedApplicable
    {
        IColliderController _colliderController;
        ITowerObservable _towerObservable;

        TowerType _towerType;

        [Inject]
        public void Construct(
            ICharacterComponentCollection owner,
            IUpdateObservable updateObservable,
            IColliderController colliderController,
            ITowerObservable towerObservable
         )
        {
            _damagedOwner = owner;
            _updateObservable = updateObservable;
            _colliderController = colliderController;
            _towerObservable = towerObservable;
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
                    _hitAttacks.Add(attackCollider);
                })
                .AddTo(_disposables);

            // TowerType
            _towerObservable.TowerType
                .Skip(1)
                .Subscribe(type =>
                {
                    _towerType = type;
                })
                .AddTo(_disposables);

            base.InternalInit();

            //TODO: �e�X�g�R�[�h
            AllHitTest();
        }

        protected override void InternalDoHit(IAttackApplicator attackApplicator)
        {
            if (_towerType == TowerType.ENEMY && attackApplicator.AttackOrigin is IPlayerComponentCollection == false) return;
            if (_towerType == TowerType.PLAYER && attackApplicator.AttackOrigin is IEnemyComponentCollection == false) return;
            base.InternalDoHit(attackApplicator);
        }

        protected override void OnHitTest(int? damage = null)
        {
            var hitData = new HitData
            {
                DamagedObject = _damagedOwner,
                Damage = damage ?? 0,
                HitType = HitType.NORMAL // Tower�͖�����
            };
            _onHit.OnNext(hitData);
            _damagedOwner.Health.Update(-hitData.Damage);
        }
    }
}
