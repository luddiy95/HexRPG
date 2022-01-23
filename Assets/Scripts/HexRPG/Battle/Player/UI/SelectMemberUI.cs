using System;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Player.UI
{
    public class SelectMemberUI : AbstractCustomComponentBehaviour, ICharacterUI
    {
        ISelectUI _selectUI;

        IObservable<Unit> ICharacterUI.OnBack => _onBack;
        ISubject<Unit> _onBack = new Subject<Unit>();

        int _selectedIndex = -1;
        int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex >= 0) _selectUI.UpdateOptionBtnSelectedStatus(_selectedIndex, false);
                _selectedIndex = value;
                if(_selectedIndex >= 0) _selectUI.UpdateOptionBtnSelectedStatus(_selectedIndex, true);
            }
        }

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ICharacterUI>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _selectUI);

            // 戻るボタン押したらSkill選択へ
            _selectUI.RegisterBtnBackEvent(() =>
            {
                CancelSelection();
                _onBack.OnNext(Unit.Default);
            });
        }

        void ICharacterUI.Bind(ICustomComponentCollection character)
        {
            if (character.QueryInterface(out IMemberObservable memberObservable))
            {
                var memberList = memberObservable.MemberList;

                IProfileSetting profileSetting = null;
                _selectUI.UpdateOptionBtnSprite(
                    memberList
                        .Where(member => member.QueryInterface(out profileSetting))
                        .Select(member => profileSetting.OptionIcon).ToArray()
                );

                // 各ボタンタップイベント登録
                _selectUI.RegisterSelectOptionEvent(index =>
                {
                    if (index > memberList.Length - 1) return;
                    SelectedIndex = index;
                });

                if (character.QueryInterface(out ICharacterInput input))
                {
                    input.OnFire
                        .Where(_ => SelectedIndex >= 0)
                        .Subscribe(_ =>
                        {
                            memberObservable.ChangeMember(SelectedIndex);
                            CancelSelection();
                            _onBack.OnNext(Unit.Default);
                        })
                        .AddTo(this);
                }
            }
        }

        void ICharacterUI.SwitchShow(bool isShow)
        {
            gameObject.SetActive(isShow);
        }
        
        void CancelSelection()
        {
            if (SelectedIndex >= 0)
            {
                _selectUI.UpdateOptionBtnSelectedStatus(SelectedIndex, false);
                SelectedIndex = -1;
            }
        }
    }
}
