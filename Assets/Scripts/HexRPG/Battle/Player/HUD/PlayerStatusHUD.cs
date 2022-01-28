using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    public class PlayerStatusHUD : AbstractCustomComponentBehaviour, ICharacterHUD
    {
        [SerializeField] Image _icon;

        [SerializeField] GameObject _healthGauge;
        [SerializeField] GameObject _mentalGauge;

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
            if (!Owner.QueryInterface(out IComponentCollectionFactory factory)) return;

            ICharacterHUD characterHUD = null;
            var healthGauge = factory.CreateComponentCollectionWithoutInstantiate(_healthGauge, null, null);
            if (healthGauge.QueryInterface(out characterHUD)) characterHUD.Bind(chara);
            var mentalGauge = factory.CreateComponentCollectionWithoutInstantiate(_mentalGauge, null, null);
            if (mentalGauge.QueryInterface(out characterHUD)) characterHUD.Bind(chara);

            if(chara.QueryInterface(out IMemberObservable memberObservable))
            {
                memberObservable.CurMember
                    .Where(member => member != null)
                    .Subscribe(member => {
                        if (member.QueryInterface(out IProfileSetting profile))
                        {
                            SetStatusIcon(profile.StatusIcon);
                        }
                    })
                    .AddTo(this);
            }
        }

        #region View

        void SetStatusIcon(Sprite sprite) => _icon.sprite = sprite;

        #endregion
    }
}
