using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.Panel
{
    public abstract class SelectBasePanelView : MonoBehaviour
    {
        [SerializeField] protected GameObject _btnDecide;
        public GameObject BtnDecide => _btnDecide;
        [SerializeField] GameObject _btnBack;
        public GameObject BtnBack => _btnBack;

        [SerializeField] protected Transform _optionBtnRoot;
        public Transform OptionBtnRoot => _optionBtnRoot;
        [SerializeField] Sprite _optionBtnDefaultSprite;
        [SerializeField] Sprite _optionBtnSelectedSprite;

        public abstract void OpenSelectPanel();

        public abstract void CloseSelectPanel();

        public virtual void SetOptionBtnSelectedStatus(int index, bool isSelected)
        {
            if(isSelected) _optionBtnRoot.GetChild(index).GetChild(0).GetComponent<Image>().sprite = _optionBtnSelectedSprite;
            else _optionBtnRoot.GetChild(index).GetChild(0).GetComponent<Image>().sprite = _optionBtnDefaultSprite;

        }
    }
}
