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
                var max = memberOwner.SkillPoint.Max;
                SetGauge(max, max);

                _disposables.Clear();
                memberOwner.SkillPoint.Current
                    .Subscribe(amount => UpdateAmount(amount))
                    .AddTo(_disposables);
            }
        }
    }
}
