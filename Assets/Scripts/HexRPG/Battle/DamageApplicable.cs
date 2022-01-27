using System;
using UniRx;

namespace HexRPG.Battle
{
    public interface IDamageApplicable : IFeature
    {
        IObservable<HitData> OnHit { get; }
    }

    public class DamageApplicable : AbstractCustomComponentBehaviour, IDamageApplicable
    {
        public IObservable<HitData> OnHit => _onHit;
        readonly ISubject<HitData> _onHit = new Subject<HitData>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IDamageApplicable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out ITransformController transformController);
            Owner.QueryInterface(out IUpdateObservable updateObservable);

            // �_���[�W����
            updateObservable
                .OnUpdate((int)UPDATE_ORDER.DAMAGED)
                .Subscribe(_ =>
                {
                    var landedHex = transformController.GetLandedHex();
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
            if (attackApplicator.TryMarkAsHit(Owner) == false)
            {
                return;
            }

            var hitData = new HitData
            {
                AttackApplicator = attackApplicator,
                DamagedObject = Owner
            };

            // health �����炷
            if (Owner.QueryInterface(out IHealth health) == true)
            {
                hitData.Damage = attackApplicator.CurrentSetting.Power;
                health.Update(-hitData.Damage);
            }

            // �R�[���o�b�N
            _onHit.OnNext(hitData);
            attackApplicator.NotifyAttackHit(hitData);
        }
    }

    public struct HitData
    {
        public IAttackApplicator AttackApplicator { get; set; }
        public ICustomComponentCollection DamagedObject { get; set; }
        public int Damage { get; set; }
    }

}