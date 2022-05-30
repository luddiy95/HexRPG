using UnityEngine;
using TMPro;

namespace HexRPG.Battle.HUD
{
    public class DamagedDisplay : MonoBehaviour
    {
        public RectTransform RectTransform => _rectTransform;
        RectTransform _rectTransform;
        public Vector2 AnchoredPos { set => _rectTransform.anchoredPosition = value; }

        TextMeshProUGUI _text;
        public int Damage { set => _text.text = value.ToString(); }

        public Material Material { set => _text.fontMaterial = value; }

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _text = GetComponent<TextMeshProUGUI>();
        }
    }
}
