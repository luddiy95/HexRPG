using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace HexRPG.Battle.Player.UI
{
    public class SelectMemberUI : MonoBehaviour, ICharacterUI
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

        void Awake()
        {
            _selectUI = GetComponent<ISelectUI>();
        }

        void Start()
        {
            // 戻るボタン押したらSkill選択へ
            _selectUI.RegisterBtnBackEvent(() =>
            {
                CancelSelection();
                _onBack.OnNext(Unit.Default);
            });
        }

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                var memberList = playerOwner.MemberObservable.MemberList;

                _selectUI.UpdateOptionBtnSprite(
                    memberList
                        .Select(member => member.ProfileSetting.OptionIcon).ToArray()
                );

                // 各ボタンタップイベント登録
                _selectUI.RegisterSelectOptionEvent(index =>
                {
                    if (index > memberList.Length - 1) return;
                    SelectedIndex = index;
                });

                playerOwner.CharacterInput.OnFire
                    .Where(_ => SelectedIndex >= 0)
                    .Subscribe(_ =>
                    {
                        playerOwner.MemberController.ChangeMember(SelectedIndex);
                        CancelSelection();
                        _onBack.OnNext(Unit.Default);
                    })
                    .AddTo(this);
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
