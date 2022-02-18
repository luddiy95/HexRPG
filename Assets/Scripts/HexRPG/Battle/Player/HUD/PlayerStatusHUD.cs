using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    public class PlayerStatusHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] Image _icon;

        [SerializeField] GameObject _healthGauge;
        [SerializeField] GameObject _mentalGauge;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            var healthGauge = _healthGauge.GetComponent<ICharacterHUD>();
            var mentalGauge = _mentalGauge.GetComponent<ICharacterHUD>();
            healthGauge.Bind(chara);
            mentalGauge.Bind(chara);

            if(chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(memberOwner => {
                        SetStatusIcon(memberOwner.ProfileSetting.StatusIcon);
                    })
                    .AddTo(this);
            }
        }

        #region View

        void SetStatusIcon(Sprite sprite) => _icon.sprite = sprite;

        #endregion
    }
}
