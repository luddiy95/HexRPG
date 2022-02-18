using UniRx;
using UnityEngine;

namespace HexRPG.Battle.Enemy.HUD
{
    public class EnemyHealthGauge : AbstractGaugeBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            //TODO:
            /*
            if(chara.QueryInterface(out IHealth health))
            {
                SetGauge(health.Max, health.Max);

                health.Current
                    .Subscribe(v => {
                        UpdateAmount(v);
                    })
                    .AddTo(this);
            }
            */
        }
    }
}
