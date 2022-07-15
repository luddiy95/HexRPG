using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Stage.Tower.HUD
{
    using Battle.HUD;

    public class TowerHealthGaugeHUD : AbstractGaugeBehaviour, ICharacterHUD
    {
        BattleData _battleData;

        [SerializeField] Image _gaugeAmount;

        [Inject]
        public void Construct(
            BattleData battleData
        )
        {
            _battleData = battleData;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if(chara is ITowerComponentCollection towerOwner)
            {
                var health = chara.Health;
                SetGauge(health.Max, health.Max);
                health.Current
                    .Subscribe(v =>
                    {
                        UpdateAmount(v);
                    })
                    .AddTo(_disposables);

                towerOwner.TowerObservable.TowerType
                    .Subscribe(type =>
                    {
                        _gaugeAmount.sprite = (type == TowerType.PLAYER) ? 
                            _battleData.playerTowerHealthGaugeAmountSprite : _battleData.enemyTowerHealthGaugeAmountSprite;
                    })
                    .AddTo(_disposables);
            }
        }
    }
}
