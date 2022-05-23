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

            // �Փ˃L���[�C���O
            this.OnTriggerEnterAsObservable()
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
                .AddTo(this);

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

            // �q�b�g�ς݃}�[�N���s�����łɃq�b�g���Ă�
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

            // �R�[���o�b�N
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);

            //TODO: �y��������zIPlayerComponentCollection����Ȃ���IMemberComponentCollection����Ȃ��H(����DamagedBehaviour��Player�ɃA�^�b�`����Ă��邪�eMember����Ȃ��H)
            //TODO: Enemy��Combat�����݂��Ȃ�����AttackEnable��Hex�o�R�����̂���Collider������Ȃ�->Player�ɃA�^�b�`����Ă���H
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