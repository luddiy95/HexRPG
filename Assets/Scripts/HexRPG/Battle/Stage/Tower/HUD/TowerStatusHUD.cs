using UnityEngine;
using UniRx;
using Zenject;
using TMPro;

namespace HexRPG.Battle.Stage.Tower.HUD
{
    using Battle.HUD;

    public class TowerStatusHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] GameObject _healthGauge;
        [SerializeField] TextMeshProUGUI _hpAmountText;

        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            if (_healthGauge.TryGetComponent(out ICharacterHUD hud)) hud.Bind(character);
            
            character.Health.Current
                .Subscribe(hp => _hpAmountText.text = hp.ToString())
                .AddTo(this);
        }

        public class Factory : PlaceholderFactory<TowerStatusHUD>
        {

        }
    }
}
