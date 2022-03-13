using UnityEngine;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    public class MemberStatusHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] GameObject _healthGauge;
        [SerializeField] GameObject _mentalGauge;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            var healthGauge = _healthGauge.GetComponent<ICharacterHUD>();
            var mentalGauge = _mentalGauge.GetComponent<ICharacterHUD>();
            healthGauge.Bind(chara);
            mentalGauge.Bind(chara);
        }
    }
}
