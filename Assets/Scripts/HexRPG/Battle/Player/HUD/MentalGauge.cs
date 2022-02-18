using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Member;

    public class MentalGauge : AbstractGaugeBehaviour, ICharacterHUD
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            void SetUpMemberChanged(IMemberComponentCollection member)
            {
                _disposables.Clear();

                var mental = member.Mental;
                SetGauge(mental.Max, mental.Current.Value);

                mental.Current
                    .Subscribe(v => UpdateAmount(v))
                    .AddTo(_disposables);
            }

            if (chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(memberOwner => {
                        SetUpMemberChanged(memberOwner);
                    })
                    .AddTo(this);
            }
        }
    }
}
