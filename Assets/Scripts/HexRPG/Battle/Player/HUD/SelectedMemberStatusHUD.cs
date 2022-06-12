using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using TMPro;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;
    using Member;

    public class SelectedMemberStatusHUD : MonoBehaviour, ICharacterHUD
    {
        IBattleObservable _battleObservable;
        BattleData _battleData;

        [SerializeField] Image _icon;
        [SerializeField] GameObject _healthGauge;
        [SerializeField] TextMeshProUGUI _hpAmountText;
        [SerializeField] GameObject _skillPointHUD;
        [SerializeField] Image _iconAttribute;

        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            BattleData battleData
        )
        {
            _battleObservable = battleObservable;
            _battleData = battleData;
        }

        void Start()
        {
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    playerOwner.DieObservable.OnFinishDie
                        .Subscribe(_ => _skillPointHUD.SetActive(false))
                        .AddTo(this);
                })
                .AddTo(this);
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            _disposables.Clear();
            if (chara is IMemberComponentCollection memberOwner)
            {
                var profile = memberOwner.ProfileSetting;
                // Icon
                _icon.sprite = profile.Icon;

                // HealthGauge
                _healthGauge.GetComponent<ICharacterHUD>().Bind(memberOwner);
                memberOwner.Health.Current
                    .Subscribe(hp => _hpAmountText.text = hp.ToString())
                    .AddTo(_disposables);

                // SkillPoint
                _skillPointHUD.GetComponent<ICharacterHUD>().Bind(memberOwner);

                // Attribute
                if (_battleData.attributeIconMap.Table.TryGetValue(profile.Attribute, out Sprite sprite)) _iconAttribute.sprite = sprite;
            }
        }

        void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
