using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public class MentalGauge : AbstractGaugeBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if(chara is IMemberComponentCollection memberOwner)
            {
                var mental = memberOwner.Mental;
                SetGauge(mental.Max, mental.Current.Value);

                mental.Current
                    .Subscribe(v => UpdateAmount(v))
                    .AddTo(this);
            }
        }
    }
}
