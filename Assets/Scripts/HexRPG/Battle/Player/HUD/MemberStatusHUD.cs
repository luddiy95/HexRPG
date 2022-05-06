using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class MemberStatusHUD : MonoBehaviour, ICharacterHUD
    {
        BattleData _battleData;

        [SerializeField] Image _background;
        [SerializeField] Image _icon; 
        [SerializeField] GameObject _healthGauge;
        [SerializeField] Text _skillPoint;
        [SerializeField] Transform _skillList;
        [SerializeField] GameObject _btnChange;

        [Inject]
        public void Construct(
            BattleData battleData
        )
        {
            _battleData = battleData;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IMemberComponentCollection memberOwner)
            {
                // Icon
                _icon.sprite = memberOwner.ProfileSetting.Icon;

                // HealthGauge
                var healthGauge = _healthGauge.GetComponent<ICharacterHUD>();
                healthGauge.Bind(chara);

                // SkillPoint
                memberOwner.SkillPoint.Current
                    .Subscribe(sp => _skillPoint.text = sp.ToString())
                    .AddTo(this);

                // SkillList
                var skillList = memberOwner.SkillSpawnObservable.SkillList;
                for (int i = 0; i < _skillList.childCount; i++)
                {
                    var child = _skillList.GetChild(i);
                    if (i > skillList.Length - 1)
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }
                    var skillSetting = skillList[i].SkillSetting;
                    child.GetChild(0).GetComponent<Image>().sprite = skillSetting.Icon;
                    child.GetChild(1).GetComponent<Text>().text = skillSetting.Cost.ToString();
                }

                // CurMember?
                memberOwner.SelectedObservable.IsSelected
                    .Subscribe(isSelected =>
                    {
                        if (isSelected)
                        {
                            _background.material = _battleData.IconMemberBackgroundSelected;
                            transform.localScale = Vector3.one * 1.2f;
                        }
                        else
                        {
                            _background.material = _battleData.IconMemberBackgroundDefault;
                            transform.localScale = Vector3.one;
                        }
                        _btnChange.SetActive(!isSelected);
                    })
                    .AddTo(this);
            }
        }
    }
}
