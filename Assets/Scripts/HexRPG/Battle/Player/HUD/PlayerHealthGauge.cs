using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Member;

    public class PlayerHealthGauge : AbstractGaugeBehaviour, ICharacterHUD
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            void SetUpMemberChanged(IMemberComponentCollection member)
            {
                _disposables.Clear();

                var health = member.Health;
                SetGauge(health.Max, health.Current.Value);

                health.Current
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
