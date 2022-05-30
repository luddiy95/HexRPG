using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class GaugeBar : MonoBehaviour, IGauge
    {
        [SerializeField] RectTransform _gaugeAmount;

        float _defaultAmountWidth;

        float _scaleX;

        protected int _maxAmount;

        void IGauge.Set(int amount)
        {
            if (amount < 0) amount = 0;
            if (amount > _maxAmount) amount = _maxAmount;

            _gaugeAmount.sizeDelta = new Vector2(_defaultAmountWidth * amount / _maxAmount, _gaugeAmount.sizeDelta.y);
        }

        protected virtual void Awake()
        {
            _defaultAmountWidth = _gaugeAmount.sizeDelta.x;
            _scaleX = GetComponent<RectTransform>().localScale.x;
        }

        void IGauge.Init(int maxAmount)
        {
            _maxAmount = maxAmount;
            (this as IGauge).Set(maxAmount);
        }
    }
}
