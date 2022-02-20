using UniRx;
using Zenject;

namespace HexRPG.Battle.Enemy.HUD
{
    public class EnemyHealthGauge : AbstractGaugeBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if(chara is IEnemyComponentCollection enemyOwner)
            {
                var health = enemyOwner.Health;
                SetGauge(health.Max, health.Max);

                health.Current
                    .Subscribe(v => {
                        UpdateAmount(v);
                    })
                    .AddTo(this);
            }
        }

        public class Factory : PlaceholderFactory<EnemyHealthGauge>
        {

        }
    }
}
