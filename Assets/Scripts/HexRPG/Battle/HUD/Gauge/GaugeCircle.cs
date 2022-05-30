using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class GaugeCircle : MonoBehaviour, IGauge
    {
        [SerializeField] RectTransform _gauge;
        [SerializeField] bool isClockwiseDecrease; // å∏è≠Ç∑ÇÈÇ∆Ç´Ç…ÉQÅ[ÉWÇéûåvâÒÇËÇ…âÒÇ∑Ç©

        protected int _maxAmount;
        void IGauge.Set(int amount)
        {

                if (amount < 0) amount = 0;
                if (amount > _maxAmount) amount = _maxAmount;

                var rotateDir = isClockwiseDecrease ? -1 : 1;
        _gauge.rotation = Quaternion.Euler(0, 0, rotateDir* 90f * (_maxAmount - amount) / _maxAmount);
        }

        void IGauge.Init(int maxAmount)
        {
            _maxAmount = maxAmount;
            (this as IGauge).Set(maxAmount);
        }
    }
}
