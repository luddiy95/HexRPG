using UnityEngine;

namespace HexRPG.Battle
{
    public class Gauge : MonoBehaviour
    {
        [SerializeField] RectTransform gauge;
        RectTransform gaugeStart;
        RectTransform gaugeAmount;
        RectTransform gaugeEnd;

        float defaultAmountWidth;

        float scaleX;

        protected int maxAmount;
        protected int amount;
        public int Amount
        {
            get { return amount; }
            set
            {
                amount = value;

                gaugeStart.gameObject.SetActive(amount > 0);
                gaugeEnd.gameObject.SetActive(amount >= maxAmount);

                if (amount < 0) amount = 0;
                if (amount > maxAmount) amount = maxAmount;

                gaugeAmount.sizeDelta = new Vector2(defaultAmountWidth * amount / maxAmount, gaugeAmount.sizeDelta.y);
            }
        }

        protected virtual void Awake()
        {
            gaugeStart = gauge.transform.GetChild(0).GetComponent<RectTransform>();
            gaugeAmount = gauge.transform.GetChild(1).GetComponent<RectTransform>();
            gaugeEnd = gauge.transform.GetChild(2).GetComponent<RectTransform>();

            defaultAmountWidth = gaugeAmount.sizeDelta.x;

            scaleX = GetComponent<RectTransform>().localScale.x;
        }

        public virtual void Init(int maxAmount)
        {
            this.maxAmount = maxAmount;
            amount = maxAmount;
        }

        public Vector3 getCanvasGainPos(int amount)
        {
            return gaugeAmount.position;
        }
    }
}
