using UnityEngine;
using TMPro;

namespace HexRPG.Battle.HUD
{
    public class DamagedDisplayHUD : AbstractPoolableMonoBehaviour<DamagedDisplayHUD>
    {
        public RectTransform RectTransform => _rectTransform ? _rectTransform : _rectTransform = GetComponent<RectTransform>();
        RectTransform _rectTransform;
        public Vector2 AnchoredPos { 
            set
            {
                RectTransform.anchoredPosition = value;
            }
        }

        TextMeshProUGUI Text => _text ? _text : _text = GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI _text;

        public int Damage { set => Text.text = value.ToString(); }
        public Material Material { set => Text.fontMaterial = value; }
    }
}
