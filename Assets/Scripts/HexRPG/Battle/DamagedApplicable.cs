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

            // �_���[�W����
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    // Combat�U��
                    foreach(var attackCollider in _hitAttacks)
                    {
                        DoHit(attackCollider.AttackApplicator);
                    }
                    _hitAttacks.Clear();

                    // Skill�U��
                    var landedHex = _transformController.GetLandedHex();
                    foreach (var attackApplicator in landedHex.AttackApplicatorList)
                    {
                        if (attackApplicator.AttackOrigin == _damagedOwner) continue;
                        DoHit(attackApplicator);
                    }

                    //TODO: �e�X�g�R�[�h
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        var hitData = new HitData
                        {
                            DamagedObject = _damagedOwner,
                            Damage = 10,
                            HitType = HitType.WEAK
                        };
                        _onHit.OnNext(hitData);
                        if (_damagedOwner is IPlayerComponentCollection playerOwner)
                            playerOwner.MemberObservable.CurMember.Value.Health.Update(-hitData.Damage);
                        if (_damagedOwner is IEnemyComponentCollection enemyOwner)
                            enemyOwner.Health.Update(-hitData.Damage);
                    }
                })
                .AddTo(_disposables);
        }

        void DoHit(IAttackApplicator attackApplicator)
        {
            if ((attackApplicator.AttackOrigin is IEnemyComponentCollection) == _isEnemy) return;

            if(_damagedOwner is IEnemyComponentCollection enemyOwner)
            {
                //! Enemy�̏ꍇ�ADamaged�X�e�[�g���͍U�����󂯂Ȃ�
                if (enemyOwner.ActionStateObservable.CurrentState.Value.Type == ActionStateType.DAMAGED) return;
            }

            // �q�b�g�ς݃}�[�N���s�����łɃq�b�g���Ă�
            if (attackApplicator.TryMarkAsHit(_damagedOwner) == false) return;

            //TODO: Critical���ǂ���(Critical��Weak/Resist�͗������Ȃ�)
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

            // �R�[���o�b�N
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);

            //! Enemy��Combat�����݂��Ȃ�����AttackEnable��Hex�o�R�����̂���Collider������Ȃ�->Player�ɃA�^�b�`����Ă���
            if (_damagedOwner is IPlayerComponentCollection player)
                player.MemberObservable.CurMember.Value.Health.Update(-hitData.Damage);
            if (_damagedOwner is IEnemyComponentCollection enemy)
                enemy.Health.Update(-hitData.Damage);
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