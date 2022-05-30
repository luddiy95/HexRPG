using UnityEngine;
using UnityEditor;
using System.Linq;
using Zenject;

namespace HexRPG.Battle.Enemy.HUD
{
    using Battle.HUD;

    public class EnemyHealthGaugeHUD : HealthGaugeHUD
    {
        DisplayDataContainer _displayDataContainer;
        IFloatingHUD _floatingHUD;

        [Inject]
        public void Construct(
            DisplayDataContainer displayDataContainer
        )
        {
            _displayDataContainer = displayDataContainer;
        }

        protected override void Awake()
        {
            _floatingHUD = GetComponent<IFloatingHUD>();

            base.Awake();
        }

        protected override void OnBind(ICharacterComponentCollection chara)
        {
            if (chara is IEnemyComponentCollection) // Œ»óFloating‚ÈHealthGauge‚ÍEnemy‚Ì‚Ý
            {
                _name = chara.ProfileSetting.Name;
                UpdateOffset();
            }

            base.OnBind(chara);
        }

        public void UpdateOffset()
        {
            var data = _displayDataContainer.displayDataMap.FirstOrDefault(data => data.name == _name);
            if (data != null)
            {
                _floatingHUD.Offset = data.gaugeOffset;
            }
        }

#if UNITY_EDITOR

        string _name;

        [CustomEditor(typeof(EnemyHealthGaugeHUD))]
        public class EnemyHealthGaugeHUDInspector : Editor
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

                if (GUILayout.Button("UpdateData")) ((EnemyHealthGaugeHUD)target).UpdateOffset();
            }
        }

#endif
    }
}
