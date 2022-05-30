using UnityEngine;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class DamagedPanelParentHUD : MonoBehaviour, ICharacterHUD
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

        public class Factory : PlaceholderFactory<DamagedPanelParentHUD>
        {

        }
    }
}
