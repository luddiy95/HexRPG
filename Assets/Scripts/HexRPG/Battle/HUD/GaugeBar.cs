using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class GaugeBar : MonoBehaviour, IGauge
    {
        [SerializeField] RectTransform _gauge;
        RectTransform _gaugeStart;
        RectTransform _gaugeAmount;
        RectTransform _gaugeEnd;

        float _defaultAmountWidth;

        float _scaleX;

        protected int _maxAmount;
        protected int _amount;
        int IGauge.Amount
        {
            set
            {
                _amount = value;

                _gaugeStart.gameObject.SetActive(_amount > 0);
                _gaugeEnd.gameObject.SetActive(_amount >= _maxAmount);

                if (_amount < 0) _amount = 0;
                if (_amount > _maxAmount) _amount = _maxAmount;

                _gaugeAmount.sizeDelta = new Vector2(_defaultAmountWidth * _amount / _maxAmount, _gaugeEnd.sizeDelta.y);
            }
        }

        protected virtual void Awake()
        {
            _gaugeStart = _gauge.transform.GetChild(0).GetComponent<RectTransform>();
            _gaugeAmount = _gauge.transform.GetChild(1).GetComponent<RectTransform>();
            _gaugeEnd = _gauge.transform.GetChild(2).GetComponent<RectTransform>();

            _defaultAmountWidth = _gaugeAmount.sizeDelta.x;

            _scaleX = GetComponent<RectTransform>().localScale.x;
        }

        void IGauge.Init(int maxAmount)
        {
            _maxAmount = maxAmount;
            _amount = maxAmount;
        }
    }
}
