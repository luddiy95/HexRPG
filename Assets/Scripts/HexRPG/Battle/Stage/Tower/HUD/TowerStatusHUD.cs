using UnityEngine;
using UniRx;
using Zenject;
using TMPro;
using System.Linq;

namespace HexRPG.Battle.Stage.Tower.HUD
{
    using Battle.HUD;

    public class TowerStatusHUD : MonoBehaviour, ICharacterHUD
    {
        DisplayDataContainer _displayDataContainer;

        [SerializeField] GameObject _healthGauge;
        [SerializeField] TextMeshProUGUI _hpAmountText;

        ITrackingHUD _trackingHUD;
        string _name;

        [Inject]
        public void Construct(
            DisplayDataContainer displayDataContainer
        )
        {
            _displayDataContainer = displayDataContainer;
        }

        void Awake()
        {
            _trackingHUD = GetComponent<ITrackingHUD>();
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            if (_healthGauge.TryGetComponent(out ICharacterHUD hud)) hud.Bind(character);
            
            character.Health.Current
                .Subscribe(hp => _hpAmountText.text = hp.ToString())
                .AddTo(this);

            // Offset
            _name = "Tower";
            UpdateOffset();
        }

        public void UpdateOffset()
        {
            var data = _displayDataContainer.displayDataMap.FirstOrDefault(data => data.name == _name);
            if (data != null)
            {
                _trackingHUD.Offset = data.statusOffset;
            }
        }

        public class Factory : PlaceholderFactory<TowerStatusHUD>
        {

        }
    }
}
