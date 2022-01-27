using UnityEngine;

namespace HexRPG.Battle
{
    public class AbstractGaugeComponentBehaviour : AbstractCustomComponentBehaviour
    {
        [SerializeField] Gauge _gauge;

        #region View

        protected void SetGauge(int max, int amount)
        {
            _gauge.Init(max);
            _gauge.Amount = amount;
        }

        protected void UpdateAmount(int amount) => _gauge.Amount = amount;

        #endregion
    }
}
