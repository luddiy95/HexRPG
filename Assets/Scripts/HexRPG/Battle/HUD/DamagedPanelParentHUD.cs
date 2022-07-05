using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class DamagedPanelParentHUD : AbstractPoolableMonoBehaviour<DamagedPanelParentHUD>, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            for (int i = 0; i < transform.childCount; i++)
            {
                var huds = transform.GetChild(i).GetComponents<ICharacterHUD>();
                foreach (var hud in huds) hud.Bind(character);
            }
        }
    }
}
