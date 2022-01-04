using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.Panel
{
    using Character.Skill;

    [RequireComponent(typeof(SelectSkillPanelPresenter))]
    public class SelectSkillPanelView : SelectBasePanelView
    {
        [SerializeField] GameObject _btnChangeCharacter;
        public GameObject BtnChangeCharacter => _btnChangeCharacter;

        public void UpdateSkillBtnList(Character.Character character)
        {
            for (int i = 0; i < _optionBtnRoot.childCount; i++)
            {
                if (i > character.SkillList.Count - 1) break;
                BaseSkill skillData = character.SkillList[i];
                Transform skillBtn = _optionBtnRoot.GetChild(i);
                Image icon = skillBtn.GetChild(1).GetComponent<Image>();
                icon.sprite = skillData.Icon;
            }
        }

        public override void OpenSelectPanel()
        {
            _btnDecide.transform.localScale = new Vector3(1, 1, 1);
            gameObject.SetActive(true);
        }

        public override void CloseSelectPanel()
        {
            _btnDecide.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            gameObject.SetActive(false);
        }
    }
}
