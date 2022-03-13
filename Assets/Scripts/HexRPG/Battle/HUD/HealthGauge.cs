using UniRx;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class HealthGauge : AbstractGaugeBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            var health = chara.Health;
            SetGauge(health.Max, health.Max);

            health.Current
                .Subscribe(v =>
                {
                    UpdateAmount(v);
                })
                .AddTo(this);
        }

        public class Factory : PlaceholderFactory<HealthGauge>
        {

        }
    }
}
