using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public interface IMemberHUD : ICharacterHUD
    {
        void SwitchShowBtnChange(bool show);
    }

    public class MemberStatusHUD : MonoBehaviour, IMemberHUD
    {
        [SerializeField] Image _icon; 
        [SerializeField] GameObject _healthGauge;
        [SerializeField] GameObject _skillPointHUD;
        [SerializeField] GameObject _btnChange;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IMemberComponentCollection memberOwner)
            {
                // Icon
                _icon.sprite = memberOwner.ProfileSetting.Icon;

                // HealthGauge
                _healthGauge.GetComponent<ICharacterHUD>().Bind(memberOwner);

                // SkillPoint
                _skillPointHUD.GetComponent<ICharacterHUD>().Bind(memberOwner);

                // SkillList
                /*
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
                */
            }
        }

        void IMemberHUD.SwitchShowBtnChange(bool show)
        {
            _btnChange.SetActive(show);
        }
    }
}
