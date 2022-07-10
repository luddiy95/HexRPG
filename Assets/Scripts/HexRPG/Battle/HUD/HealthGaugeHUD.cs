using UniRx;

namespace HexRPG.Battle.HUD
{
    public class HealthGaugeHUD : AbstractGaugeBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            var health = chara.Health;
            SetGauge(health.Max, health.Max);

            _disposable?.Dispose();
            _disposable = health.Current
                .Subscribe(v =>
                {
                    UpdateAmount(v);
                });
        }
    }
}
