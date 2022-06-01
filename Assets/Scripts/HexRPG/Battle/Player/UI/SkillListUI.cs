using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        List<ISkillUI> _skillUIList = new List<ISkillUI>();
        ISkillSetting[] _curMemberSkillSettings;

        void Start()
        {
            SwitchBtnDecideEnable(false);
            SwitchBtnCancelVisible(false);

            for (int i = 0; i < _skillBtnList.childCount; i++) _skillUIList.Add(_skillBtnList.GetChild(i).GetComponent<ISkillUI>());
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
                        UpdateSkillBtn(memberOwner.SkillPoint.Current.Value);
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
                            _skillUIList[x.Previous].SwitchSelected(false);
                        }

                        if (x.Current >= 0)
                        {
                            _skillUIList[x.Current].SwitchSelected(true);
                        }

                        SwitchBtnCancelVisible(x.Current != -1);
                        SwitchBtnDecideEnable(x.Current != -1);
                    })
                    .AddTo(this);
            }
        }

        #region View

        void UpdateSkillBtn(int sp)
        {
            for (int i = 0; i < _skillUIList.Count; i++)
            {
                var skillUI = _skillUIList[i];
                skillUI.SwitchSelected(false);
                if(i > _curMemberSkillSettings.Length - 1)
                {
                    skillUI.SwitchSkillShow(false);
                    skillUI.SwitchEnable(true); // skill‚ð”ñ•\Ž¦‚É‚·‚é‚½‚ßdisableFilter‚à”ñ•\Ž¦
                    continue;
                }
                skillUI.SwitchSkillShow(true);
                UpdateIconSkillEnable(sp);
                skillUI.SetSkill(_curMemberSkillSettings[i]);
            }
        }

        void UpdateIconSkillEnable(int sp)
        {
            for(int i = 0; i < _curMemberSkillSettings.Length; i++)
            {
                _skillUIList[i].SwitchEnable(_curMemberSkillSettings[i].Cost <= sp);
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
