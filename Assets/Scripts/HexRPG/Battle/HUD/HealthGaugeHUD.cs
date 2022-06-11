using System;
using UniRx;

namespace HexRPG.Battle.HUD
{
    public class HealthGaugeHUD : AbstractGaugeBehaviour, ICharacterHUD
    {
        IDisposable _disposable;

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
