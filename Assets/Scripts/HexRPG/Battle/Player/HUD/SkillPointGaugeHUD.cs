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

                _disposables.Clear();
                memberOwner.SkillPoint.ChargeRate
                    .Subscribe(rate => UpdateAmount((int)(rate * 100)))
                    .AddTo(_disposables);
            }
        }
    }
}
