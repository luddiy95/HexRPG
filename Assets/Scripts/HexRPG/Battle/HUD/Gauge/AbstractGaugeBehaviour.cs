using UnityEngine;

namespace HexRPG.Battle.HUD
{
    public class AbstractGaugeBehaviour : MonoBehaviour
    {
        [SerializeField] GameObject _gaugeObj;
        protected IGauge _gauge;

        protected virtual void Awake()
        {
            _gauge = _gaugeObj.GetComponent<IGauge>();
        }

        #region View

        protected void SetGauge(int max, int amount)
        {
            _gauge.Init(max);
            _gauge.Set(amount);
        }

        protected void UpdateAmount(int amount) => _gauge.Set(amount);

        #endregion
    }
}
