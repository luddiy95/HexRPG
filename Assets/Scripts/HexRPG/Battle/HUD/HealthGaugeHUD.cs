using System;
using UniRx;

namespace HexRPG.Battle.HUD
{
    public class HealthGaugeHUD : AbstractGaugeBehaviour, ICharacterHUD, IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            var health = chara.Health;
            SetGauge(health.Max, health.Max);

            health.Current
                .Subscribe(v =>
                {
                    UpdateAmount(v);
                })
                .AddTo(_disposables);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
