using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.Panel
{
    public class StatusPanelView : MonoBehaviour
    {
        [SerializeField] Image _statusIcon;

        [SerializeField] HP _hp;
        [SerializeField] MP _mp;

        public void SetStatusIcon(Sprite sprite) => _statusIcon.sprite = sprite;

        public void SetHP(int maxHP, int hp)
        {
            _hp.Init(maxHP);
            _hp.Amount = hp;
        }

        public void UpdateHP(int amount) => _hp.Amount = amount;

        public void SetMP(int maxMP, int mp)
        {
            _mp.Init(maxMP);
            _mp.Amount = mp;
        }

        public void UpdateMP(int amount) => _mp.Amount = amount;
    }
}
