using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public interface IDamageApplicable
    {
        IObservable<HitData> OnHit { get; }
    }

    public class DamageApplicable : IDamageApplicable, IInitializable, IDisposable
    {
        ICharacterComponentCollection _owner;
        ITransformController _transformController;
        IUpdateObservable _updateObservable;
        IHealth _health;

        public IObservable<HitData> OnHit => _onHit;
        readonly ISubject<HitData> _onHit = new Subject<HitData>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public DamageApplicable(
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
            // �_���[�W����
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    var landedHex = _transformController.GetLandedHex();
                    foreach (var attackApplicator in landedHex.AttackApplicatorList)
                    {
                        DoHit(attackApplicator);
                    }
                })
                .AddTo(_disposables);
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
                DamagedObject = _owner
            };

            // health �����炷
            hitData.Damage = attackApplicator.CurrentSetting.Power;
            _health.Update(-hitData.Damage);

            // �R�[���o�b�N
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }

    public struct HitData
    {
        public IAttackApplicator AttackApplicator { get; set; }
        public ICharacterComponentCollection DamagedObject { get; set; }
        public int Damage { get; set; }
    }

}