using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class MemberStatusHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] Image _icon; 
        [SerializeField] GameObject _healthGauge;
        [SerializeField] GameObject _skillPointHUD;
        [SerializeField] Transform _skillList;
        [SerializeField] GameObject _btnChange;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IMemberComponentCollection memberOwner)
            {
                // Icon
                _icon.sprite = memberOwner.ProfileSetting.Icon;

                // HealthGauge
                _healthGauge.GetComponent<ICharacterHUD>().Bind(chara);

                // SkillPoint
                _skillPointHUD.GetComponent<ICharacterHUD>().Bind(chara);

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
            }
        }
    }
}
