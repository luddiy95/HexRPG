using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class SkillPointHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] Text _spAmountText;
        [SerializeField] Text _spMaxText;

        [SerializeField] GameObject _spGaugeObj;
        IGauge _spGauge;

        ISkillPoint _memberSkillPoint;

        CompositeDisposable _disposables = new CompositeDisposable();

        void Start()
        {
            _spGauge = _spGaugeObj.GetComponent<IGauge>();
            _spGauge.Init(100);
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IMemberComponentCollection memberOwner)
            {
                _memberSkillPoint = memberOwner.SkillPoint;

                _spMaxText.text = _memberSkillPoint.Max.ToString();

                _disposables.Clear();
                _memberSkillPoint.Current
                    .Subscribe(sp => _spAmountText.text = sp.ToString())
                    .AddTo(_disposables);
                _memberSkillPoint.ChargeRate
                    .Subscribe(rate => _spGauge.Set((int)(rate * 100)))
                    .AddTo(_disposables);
            }
        }

        void Destroy()
        {
            _disposables.Dispose();
        }

        int _changeAmount = 0;

#if UNITY_EDITOR

        public void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _changeAmount = EditorGUILayout.IntField(_changeAmount);
                if (GUILayout.Button("Increase")) _memberSkillPoint.Update(_changeAmount);
                if (GUILayout.Button("Decrease")) _memberSkillPoint.Update(-_changeAmount);
            }
        }

        [CustomEditor(typeof(SkillPointHUD))]
        public class SkillPointHUDInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                ((SkillPointHUD)target).OnInspectorGUI();
            }
        }

#endif
    }
}
