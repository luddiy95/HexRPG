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
                    if (attackCollider.AttackApplicator.AttackOrigin == _owner)
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
                    //TODO: Enemy�̏ꍇ��Player�̏ꍇ�Ń_���[�W���󂯂�attackOrigin���Ⴄ->isPlayer�Ȃǂ̃t���O

                    // Combat�U��
                    foreach(var attackCollider in _hitAttacks)
                    {
                        DoHit(attackCollider.AttackApplicator);
                    }

                    // Skill�U��
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
            // �q�b�g�ς݃}�[�N���s�����łɃq�b�g���Ă�
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

            // health �����炷
            _health.Update(-hitData.Damage);

            // �R�[���o�b�N
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