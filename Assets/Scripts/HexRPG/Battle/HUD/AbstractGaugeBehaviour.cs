using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class AbstractGaugeBehaviour : MonoBehaviour
    {
        [SerializeField] GameObject _gaugeObj;
        IGauge _gauge;

        void Awake()
        {
            _gauge = _gaugeObj.GetComponent<IGauge>();
        }

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
