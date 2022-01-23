using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    public class PlayerStatusPresenter : AbstractCustomComponentBehaviour, ICharacterHUD
    {
        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);
            owner.RegisterInterface<ICharacterHUD>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        void ICharacterHUD.Bind(ICustomComponentCollection character)
        {
            if(character.QueryInterface(out IHealth health) == true)
            {
                health.Current
                    .Subscribe(v =>
                    {

                    })
                    .AddTo(this);
            }
        }
    }

    public class PlayerStatusView : MonoBehaviour
    {
        [SerializeField] Image _statusIcon;

        [SerializeField] HP _hp;
        [SerializeField] MP _mp;

        public void SetStatusIcon(Sprite sprite) => _statusIcon.sprite = sprite;

        public void SetHP(int maxHP, int hp)
        {
            _hp.Init(maxHP);
            _hp.Amount = hp;
        }

        public void UpdateHP(int amount) => _hp.Amount = amount;

        public void SetMP(int maxMP, int mp)
        {
            _mp.Init(maxMP);
            _mp.Amount = mp;
        }

        public void UpdateMP(int amount) => _mp.Amount = amount;
    }
}
