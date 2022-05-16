using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class SkillPointHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] Transform _spGaugeList1;
        [SerializeField] Transform _spGaugeList2;

        [SerializeField] Text _spAmount;

        IUpdateObservable _updateObservable;

        ISkillPoint _memberSkillPoint;

        List<IGauge> _spGaugeList = new List<IGauge>();

        int _spMax = 0;
        
        float _chargeRate = 0f;
        float _chargeSpeed = 0.35f;

        [Inject]
        public void Construct(
            IUpdateObservable updateObservable
        )
        {
            _updateObservable = updateObservable;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IMemberComponentCollection memberOwner)
            {
                _memberSkillPoint = memberOwner.SkillPoint;

                // Gaugeèâä˙âª
                _spMax = _memberSkillPoint.Max;
                _spAmount.text = _spMax.ToString();

                var spGaugeObjList = new List<Transform>();
                spGaugeObjList.Add(null);
                for (int i = 4 - 1; i >= 0; i--) spGaugeObjList.Add(_spGaugeList1.GetChild(i));
                for (int i = 4 - 1; i >= 0; i--) spGaugeObjList.Add(_spGaugeList2.GetChild(i));
                for (int sp = _spMax + 1; sp <= 8; sp++) spGaugeObjList[sp].gameObject.SetActive(false);

                _spGaugeList = spGaugeObjList.Select(obj => obj?.GetComponent<IGauge>()).ToList();
                _spGaugeList.ForEach(gauge => gauge?.Init(100));

                // çwì«ìoò^
                _memberSkillPoint.Current
                    .Pairwise()
                    .Subscribe(pair =>
                    {
                        var prevSp = pair.Previous;
                        var curSp = pair.Current;

                        _spAmount.text = curSp.ToString();
                        
                        if(curSp > prevSp) for (int sp = prevSp + 1; sp <= curSp; sp++) _spGaugeList[sp].Set(100);
                        else if(curSp < prevSp) for (int sp = curSp + 1; sp <= 8; sp++) _spGaugeList[sp]?.Set(0);
                    })
                    .AddTo(this);

                // SPÉ`ÉÉÅ[ÉW
                _updateObservable.OnUpdate((int)UPDATE_ORDER.SP_CHARGE)
                    .Where(_ => _memberSkillPoint.Current.Value < _spMax)
                    .Subscribe(_ =>
                    {
                        _chargeRate += _chargeSpeed * Time.deltaTime;

                        var curSp = _memberSkillPoint.Current.Value;
                        if(curSp < _spMax) _spGaugeList[curSp + 1].Set((int)(_chargeRate * 100));

                        if(_chargeRate >= 1)
                        {
                            _chargeRate = 0;
                            _memberSkillPoint.Update(1);
                        }
                    })
                    .AddTo(this);
            }
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
