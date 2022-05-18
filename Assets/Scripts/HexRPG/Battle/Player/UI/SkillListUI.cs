using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Player.UI
{
    using Skill;
    using Battle.UI;

    public class SkillListUI : MonoBehaviour, ICharacterUI
    {
        [SerializeField] Transform _skillBtnList;

        [SerializeField] GameObject _btnCancel;
        [SerializeField] Image _btnDecide;

        [SerializeField] Sprite _btnDecideEnableSprite;
        [SerializeField] Sprite _btnDecideDisableSprite;
        [SerializeField] Sprite _skillBackgroundDefaultSprite;
        [SerializeField] Sprite _skillBackgroundSelectedSprite;

        ISkillSetting[] _curMemberSkillSettings;

        void Start()
        {
            SwitchBtnDecideEnable(false);
            SwitchBtnCancelVisible(false);
        }

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                var curMember = playerOwner.MemberObservable.CurMember;

                curMember
                    .Subscribe(memberOwner =>
                    {
                        _curMemberSkillSettings = memberOwner.SkillSpawnObservable.SkillList.Select(skill => skill.SkillSetting).ToArray();
                        UpdateSkillBtn();
                    })
                    .AddTo(this);

                curMember.Value.SkillPoint.Current
                    .Subscribe(sp =>
                    {
                        UpdateIconSkillEnable(sp);
                    })
                    .AddTo(this);

                playerOwner.SelectSkillObservable.SelectedSkillIndex
                    .Pairwise()
                    .Subscribe(x =>
                    {
                        if (x.Current != x.Previous && x.Previous >= 0)
                        {
                            UpdateBtnSelectedStatus(x.Previous, false);
                        }

                        if (x.Current >= 0)
                        {
                            UpdateBtnSelectedStatus(x.Current, true);
                        }

                        SwitchBtnCancelVisible(x.Current != -1);
                        SwitchBtnDecideEnable(x.Current != -1);
                    })
                    .AddTo(this);
            }
        }

        #region View

        void UpdateSkillBtn()
        {
            for (int i = 0; i < _skillBtnList.childCount; i++)
            {
                UpdateBtnSelectedStatus(i, false);
                var optionBtn = _skillBtnList.GetChild(i);
                var type = optionBtn.GetChild(0).GetComponent<Image>();
                var icon = optionBtn.GetChild(1).GetComponent<Image>();
                var cost = optionBtn.GetChild(3).GetComponent<Text>();
                if (i > _curMemberSkillSettings.Length - 1)
                {
                    type.gameObject.SetActive(false);
                    icon.gameObject.SetActive(false);
                    cost.gameObject.SetActive(false);
                    continue;
                }
                var skillSetting = _curMemberSkillSettings[i];
                //TODO: ëÆê´
                icon.sprite = skillSetting.Icon;
                cost.text = skillSetting.Cost.ToString();
            }
        }

        void UpdateIconSkillEnable(int sp)
        {
            for(int i = 0; i < _curMemberSkillSettings.Length; i++)
            {
                var optionBtn = _skillBtnList.GetChild(i);
                var icon = optionBtn.GetChild(1).GetComponent<Image>();
                var cost = optionBtn.GetChild(3).GetComponent<Text>();
                var skillSetting = _curMemberSkillSettings[i];
                optionBtn.GetChild(2).gameObject.SetActive(skillSetting.Cost > sp);
            }
        }

        void UpdateBtnSelectedStatus(int index, bool isSelected)
        {
            if (isSelected)
            {
                _skillBtnList.GetChild(index).GetComponent<Image>().sprite = _skillBackgroundSelectedSprite;
            }
            else
            {
                _skillBtnList.GetChild(index).GetComponent<Image>().sprite = _skillBackgroundDefaultSprite;
            }
        }

        void SwitchBtnCancelVisible(bool show)
        {
            _btnCancel.SetActive(show);
        }

        void SwitchBtnDecideEnable(bool enable)
        {
            if (enable) _btnDecide.sprite = _btnDecideEnableSprite;
            else _btnDecide.sprite = _btnDecideDisableSprite;
        }

        #endregion
    }
}
