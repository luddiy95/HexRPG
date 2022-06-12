using UnityEngine;
using UnityEngine.UI;
using Zenject;
using UniRx;
using TMPro;

namespace HexRPG.Battle.Player.HUD
{
    using Member;
    using Battle.HUD;

    public interface IMemberHUD : ICharacterHUD
    {
        GameObject BtnChange { get; }

        bool IsSelected { get; set; }
        void SwitchShowBtnChange(bool show);
    }

    public class MemberStatusHUD : MonoBehaviour, IMemberHUD
    {
        BattleData _battleData;

        [SerializeField] Image _icon;
        [SerializeField] GameObject _healthGauge;
        [SerializeField] TextMeshProUGUI _hpAmountText;
        [SerializeField] GameObject _skillPointHUD;
        [SerializeField] Transform _attribute;
        [SerializeField] GameObject _selectedFilter;
        [SerializeField] GameObject _deadFilter;
        [SerializeField] GameObject _btnChange;
        [SerializeField] GameObject _selectedArrow;

        GameObject IMemberHUD.BtnChange => _btnChange;

        CompositeDisposable _disposables = new CompositeDisposable();

        bool IMemberHUD.IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                SwitchShowSelected(_isSelected);
                UpdateShowFilter();
                (this as IMemberHUD).SwitchShowBtnChange(!_isSelected);
            }
        }
        bool _isSelected = false;

        bool _isDead = false;

        [Inject]
        public void Construct(
            BattleData battleData
        )
        {
            _battleData = battleData;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            _disposables.Clear();
            if (chara is IMemberComponentCollection memberOwner)
            {
                var profile = memberOwner.ProfileSetting;
                // Icon
                _icon.sprite = profile.Icon;

                // Filter
                chara.DieObservable.OnFinishDie
                    .Subscribe(_ =>
                    {
                        _isDead = true;
                        UpdateShowFilter();
                        (this as IMemberHUD).SwitchShowBtnChange(false);
                        _skillPointHUD.SetActive(false);
                    })
                    .AddTo(_disposables);

                // HealthGauge
                _healthGauge.GetComponent<ICharacterHUD>().Bind(memberOwner);
                memberOwner.Health.Current
                    .Subscribe(hp => _hpAmountText.text = hp.ToString())
                    .AddTo(_disposables);

                // SkillPoint
                _skillPointHUD.GetComponent<ICharacterHUD>().Bind(memberOwner);

                // Attribute
                if (_battleData.attributeIconMap.Table.TryGetValue(profile.Attribute, out Sprite sprite))
                    _attribute.GetChild(0).GetComponent<Image>().sprite = sprite;
            }
        }

        void UpdateShowFilter()
        {
            _deadFilter.SetActive(false); SwitchShowSelected(false);
            if (_isDead)
            {
                _deadFilter.SetActive(true);
                return;
            }
            if (_isSelected) SwitchShowSelected(true);
        }

        void SwitchShowSelected(bool show)
        {
            _selectedFilter.SetActive(show);
            _selectedArrow.SetActive(show);
        }

        void IMemberHUD.SwitchShowBtnChange(bool show)
        {
            if ((_isSelected || _isDead) && show) return;
            _btnChange.SetActive(show);
        }

        void OnDestroy()
        {
            _disposables.Dispose();
        }

        public class Factory : PlaceholderFactory<MemberStatusHUD>
        {

        }
    }
}
