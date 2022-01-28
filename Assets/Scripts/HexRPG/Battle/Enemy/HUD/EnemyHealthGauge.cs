using UniRx;

namespace HexRPG.Battle.Enemy.HUD
{
    public class EnemyHealthGauge : AbstractGaugeComponentBehaviour, ICharacterHUD
    {
        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ICharacterHUD>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        void ICharacterHUD.Bind(ICustomComponentCollection chara)
        {
            if(chara.QueryInterface(out IHealth health))
            {
                SetGauge(health.Max, health.Max);

                health.Current
                    .Subscribe(v => {
                        UpdateAmount(v);
                    })
                    .AddTo(this);
            }
        }
    }
}
