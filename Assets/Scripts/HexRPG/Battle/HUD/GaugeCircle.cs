using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class GaugeCircle : MonoBehaviour, IGauge
    {
        [SerializeField] RectTransform _gauge;
        [SerializeField] bool isClockwiseDecrease; // å∏è≠Ç∑ÇÈÇ∆Ç´Ç…ÉQÅ[ÉWÇéûåvâÒÇËÇ…âÒÇ∑Ç©

        protected int _maxAmount;
        protected int _amount;
        int IGauge.Amount
        {
            set
            {
                _amount = value;

                if (_amount < 0) _amount = 0;
                if (_amount > _maxAmount) _amount = _maxAmount;

                var rotateDir = isClockwiseDecrease ? -1 : 1;
                _gauge.rotation = Quaternion.Euler(0, 0, rotateDir * 90f * (_maxAmount - _amount) / _maxAmount);
            }
        }

        void IGauge.Init(int maxAmount)
        {
            _maxAmount = maxAmount;
            _amount = maxAmount;
        }
    }
}
