using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class SkillPointHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] Text _spAmountText;
        [SerializeField] Text _spMaxText;

        [SerializeField] GameObject _skillPointGauge;

        ISkillPoint _memberSkillPoint;

        CompositeDisposable _disposables = new CompositeDisposable();

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IMemberComponentCollection memberOwner)
            {
                _memberSkillPoint = memberOwner.SkillPoint;

                _spMaxText.text = _memberSkillPoint.Max.ToString();

                _disposables.Clear();
                _skillPointGauge.GetComponent<ICharacterHUD>().Bind(memberOwner);
                _memberSkillPoint.Current
                    .Subscribe(sp => _spAmountText.text = sp.ToString())
                    .AddTo(_disposables);
            }
        }

        void Destroy()
        {
            _disposables.Dispose();
        }

#if UNITY_EDITOR

        int _changeAmount = 0;

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
        public class CustomInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                ((SkillPointHUD)target).OnInspectorGUI();
            }
        }

#endif
    }
}
