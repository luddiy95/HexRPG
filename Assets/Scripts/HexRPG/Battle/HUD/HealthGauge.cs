using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class HealthGauge : AbstractGaugeBehaviour, ICharacterHUD, IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            _disposables.Clear();

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

        public class Factory : PlaceholderFactory<HealthGauge>
        {

        }
    }
}
