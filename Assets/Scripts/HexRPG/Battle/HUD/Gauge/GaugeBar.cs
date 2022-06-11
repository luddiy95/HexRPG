using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class GaugeBar : MonoBehaviour, IGauge
    {
        [SerializeField] RectTransform _gaugeAmount;

        float _defaultAmountWidth;

        protected int _maxAmount;

        void Awake()
        {
            _defaultAmountWidth = _gaugeAmount.sizeDelta.x;
        }

        void IGauge.Set(int amount)
        {
            if (amount < 0) amount = 0;
            if (amount > _maxAmount) amount = _maxAmount;

            _gaugeAmount.sizeDelta = new Vector2(_defaultAmountWidth * amount / _maxAmount, _gaugeAmount.sizeDelta.y);
        }

        void IGauge.Init(int maxAmount)
        {
            _maxAmount = maxAmount;
            (this as IGauge).Set(maxAmount);
        }
    }
}
