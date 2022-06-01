using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace HexRPG.Battle.Player.UI
{
    using Skill;

    public interface ISkillUI
    {
        void SetSkill(ISkillSetting setting);
        void SwitchSkillShow(bool isShow);
        void SwitchEnable(bool enable);
        void SwitchSelected(bool selected);
    }

    public class SkillUI : MonoBehaviour, ISkillUI
    {
        BattleData _battleData;

        Image _background;
        Image _attribute;
        Image _icon;
        GameObject _disableFilter;
        Text _cost;

        [Inject]
        public void Construct(BattleData battleData)
        {
            _battleData = battleData;
        }

        void Start()
        {
            _background = GetComponent<Image>();
            _attribute = transform.GetChild(0).GetComponent<Image>();
            _icon = transform.GetChild(1).GetComponent<Image>();
            _disableFilter = transform.GetChild(2).gameObject;
            _cost = transform.GetChild(3).GetComponent<Text>();
        }

        void ISkillUI.SetSkill(ISkillSetting setting)
        {
            _icon.sprite = setting.Icon;
            _cost.text = setting.Cost.ToString();
            if (_battleData.skillAttributeMaterialMap.Table.TryGetValue(setting.Attribute, out Material mat)) _attribute.material = mat;
        }

        void ISkillUI.SwitchSkillShow(bool isShow)
        {
            _attribute.gameObject.SetActive(isShow);
            _icon.gameObject.SetActive(isShow);
            _cost.gameObject.SetActive(isShow);
        }

        void ISkillUI.SwitchEnable(bool enable)
        {
            _disableFilter.SetActive(!enable);
        }

        void ISkillUI.SwitchSelected(bool selected)
        {
            if (selected) _background.sprite = _battleData.skillBackgroundSelectedSprite;
            else _background.sprite = _battleData.skillBackgroundDefaultSprite;
        }
    }
}
