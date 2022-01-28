using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    public class PlayerHealthGauge : AbstractGaugeComponentBehaviour, ICharacterHUD
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ICharacterHUD>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        void ICharacterHUD.Bind(ICustomComponentCollection chara)
        {
            void SetUpMemberChanged(ICustomComponentCollection member)
            {
                _disposables.Clear();

                if (member.QueryInterface(out IHealth health))
                {
                    SetGauge(health.Max, health.Current.Value);

                    health.Current
                        .Subscribe(v => UpdateAmount(v))
                        .AddTo(_disposables);
                }
            }

            if (chara.QueryInterface(out IMemberObservable memberObservable))
            {
                memberObservable.CurMember
                    .Where(member => member != null)
                    .Subscribe(member => {
                        SetUpMemberChanged(member);
                    })
                    .AddTo(this);
            }
        }
    }
}
