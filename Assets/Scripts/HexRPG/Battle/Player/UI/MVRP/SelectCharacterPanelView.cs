using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.Panel
{
    public class SelectCharacterPanelView : SelectBasePanelView
    {
        public void InitCharacterBtnList(List<Sprite> spriteList)
        {
            for (int i = 0; i < _optionBtnRoot.childCount; i++)
            {
                if (i > spriteList.Count - 1) break;
                Transform characterBtn = _optionBtnRoot.GetChild(i);
                Image icon = characterBtn.GetChild(1).GetComponent<Image>();
                icon.sprite = spriteList[i];
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
