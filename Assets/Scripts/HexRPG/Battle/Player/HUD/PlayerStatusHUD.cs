using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    public class PlayerStatusHUD : AbstractCustomComponentBehaviour, ICharacterHUD
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        [SerializeField] Image _icon;

        [SerializeField] HP _hp;
        [SerializeField] MP _mp;

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

                if(member.QueryInterface(out IProfileSetting profile))
                {
                    SetStatusIcon(profile.StatusIcon);
                }

                if (member.QueryInterface(out IHealth health))
                {
                    SetHP(health.Max, health.Current.Value);

                    health.Current
                        .Subscribe(v => UpdateHP(v))
                        .AddTo(_disposables);
                }

                if (member.QueryInterface(out IMental mental))
                {
                    SetMP(mental.Max, mental.Current.Value);

                    mental.Current
                        .Subscribe(v => UpdateMP(v))
                        .AddTo(_disposables);
                }
            }

            if(chara.QueryInterface(out IMemberObservable memberObservable))
            {
                memberObservable.CurMember
                    .Where(member => member != null)
                    .Subscribe(member => {
                        SetUpMemberChanged(member);
                    })
                    .AddTo(this);
            }
        }

        #region View

        void SetStatusIcon(Sprite sprite) => _icon.sprite = sprite;

        void SetHP(int maxHP, int hp)
        {
            _hp.Init(maxHP);
            _hp.Amount = hp;
        }

        void UpdateHP(int amount) => _hp.Amount = amount;

        void SetMP(int maxMP, int mp)
        {
            _mp.Init(maxMP);
            _mp.Amount = mp;
        }

        void UpdateMP(int amount) => _mp.Amount = amount;

        #endregion
    }
}
