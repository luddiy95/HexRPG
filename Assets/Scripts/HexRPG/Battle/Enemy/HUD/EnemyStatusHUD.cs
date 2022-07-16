using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using System.Linq;
using UniRx;
using TMPro;

namespace HexRPG.Battle.Enemy.HUD
{
    using Battle.HUD;

    public class EnemyStatusHUD : AbstractPoolableMonoBehaviour<EnemyStatusHUD>, ICharacterHUD
    {
        BattleData _battleData;
        DisplayDataContainer _displayDataContainer;

        [SerializeField] GameObject _healthGauge;
        [SerializeField] TextMeshProUGUI _hpAmountText;
        [SerializeField] Image _iconAttribute;

        ITrackingHUD _trackingHUD;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            BattleData battleData,
            DisplayDataContainer displayDataContainer
        )
        {
            _battleData = battleData;
            _displayDataContainer = displayDataContainer;
        }

        void Awake()
        {
            _trackingHUD = GetComponent<ITrackingHUD>();
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            _disposables.Clear();

            if (character is IEnemyComponentCollection)
            {
                if (_healthGauge.TryGetComponent(out ICharacterHUD hud)) hud.Bind(character);

                character.Health.Current
                    .Subscribe(hp => _hpAmountText.text = hp.ToString())
                    .AddTo(_disposables);
                
                var profile = character.ProfileSetting;
                // Attribute
                if (_battleData.attributeIconMap.Table.TryGetValue(profile.Attribute, out Sprite sprite)) _iconAttribute.sprite = sprite;
                // Offset
                _name = profile.Name;
                UpdateOffset();
            }
        }

        public void UpdateOffset()
        {
            var data = _displayDataContainer.displayDataMap.FirstOrDefault(data => data.name == _name);
            if (data != null)
            {
                _trackingHUD.Offset = data.statusOffset;
            }
        }

        void OnDestroy()
        {
            _disposables.Dispose();
        }

#if UNITY_EDITOR

        string _name;

        [CustomEditor(typeof(EnemyStatusHUD))]
        public class CustomInspector : Editor
        {
            private void OnEnable()
            {
            }

            private void OnDisable()
            {
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (GUILayout.Button("UpdateData")) ((EnemyStatusHUD)target).UpdateOffset();
            }
        }

#endif
    }
}
