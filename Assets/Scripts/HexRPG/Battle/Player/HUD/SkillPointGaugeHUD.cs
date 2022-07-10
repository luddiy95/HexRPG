using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class SkillPointGaugeHUD : AbstractGaugeBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if(chara is IMemberComponentCollection memberOwner)
            {
                SetGauge(100, 100);

                _disposable?.Dispose();
                _disposable = memberOwner.SkillPoint.ChargeRate
                    .Subscribe(rate => UpdateAmount((int)(rate * 100)));
            }
        }
    }
}
