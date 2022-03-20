using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.Player.UI
{
    using Battle.UI;

    public class SkillListUI : MonoBehaviour, ICharacterUI
    {
        [SerializeField] Transform _skillBtnList;

        [SerializeField] GameObject _btnBack;
        [SerializeField] Image _btnDecide;

        [SerializeField] Sprite _btnDecideEnableSprite;
        [SerializeField] Sprite _btnDecideDisableSprite;
        [SerializeField] Sprite _skillBackgroundDefaultSprite;
        [SerializeField] Sprite _skillBackgroundSelectedSprite;

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(memberOwner =>
                    {
                        Sprite[] skillIconList = memberOwner.SkillSpawnObservable.SkillList.Select(skill =>
                        {
                            return skill.SkillSetting.Icon;
                        }).ToArray();

                        UpdateBtnIcon(skillIconList);
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

                        SwitchBtnBackVisible(x.Current != -1);
                        SwitchBtnDecideEnable(x.Current != -1);
                    })
                    .AddTo(this);
            }
        }

        #region View

        void UpdateBtnIcon(Sprite[] skillIconList)
        {
            for (int i = 0; i < _skillBtnList.childCount; i++)
            {
                UpdateBtnSelectedStatus(i, false);
                if (i > skillIconList.Length - 1) return;
                var optionBtn = _skillBtnList.GetChild(i);
                Image icon = optionBtn.GetChild(0).GetComponent<Image>();
                icon.sprite = skillIconList[i];
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

        void SwitchBtnBackVisible(bool show)
        {
            _btnBack.SetActive(show);
        }

        void SwitchBtnDecideEnable(bool enable)
        {
            if (enable) _btnDecide.sprite = _btnDecideEnableSprite;
            else _btnDecide.sprite = _btnDecideDisableSprite;
        }

        #endregion
    }
}
