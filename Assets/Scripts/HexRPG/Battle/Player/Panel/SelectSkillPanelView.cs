using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.Panel
{
    using Character.Skill;

    [RequireComponent(typeof(SelectSkillPanelPresenter))]
    public class SelectSkillPanelView : MonoBehaviour
    {
        [SerializeField] GameObject _btnDecide;
        public GameObject BtnDecide => _btnDecide;
        [SerializeField] GameObject _btnBack;
        public GameObject BtnBack => _btnBack;

        [SerializeField] Transform _skillBtnListRoot;
        public Transform SkillBtnListRoot => _skillBtnListRoot;
        [SerializeField] Sprite _defaultSkillBtnSprite;
        [SerializeField] Sprite _selectedSkillBtnSprite;

        public void UpdateSkillBtnList(Character.Character character)
        {
            for (int i = 0; i < _skillBtnListRoot.childCount; i++)
            {
                if (i > character.SkillList.Count - 1) break;
                BaseSkill skillData = character.SkillList[i];
                Transform skillButton = _skillBtnListRoot.GetChild(i);
                Image icon = skillButton.GetChild(1).GetComponent<Image>();
                icon.sprite = skillData.Icon;
            }
        }

        public void OpenSelectSkillPanelToSkillSelect()
        {
            _btnDecide.transform.localScale = new Vector3(1, 1, 1);
            gameObject.SetActive(true);
        }

        public void CloseSelectSkillPanel()
        {
            _btnDecide.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            gameObject.SetActive(false);
        }

        public void SetSkillBtnSelectedStatus(int index, bool status)
        {
            if (status) _skillBtnListRoot.GetChild(index).GetChild(0).GetComponent<Image>().sprite = _selectedSkillBtnSprite;
            else _skillBtnListRoot.GetChild(index).GetChild(0).GetComponent<Image>().sprite = _defaultSkillBtnSprite;
        }
    }
}
