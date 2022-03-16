using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.UI
{
    using Battle.UI;

    public class MemberListUI : MonoBehaviour, ICharacterUI
    {
        IObservable<Unit> ICharacterUI.OnBack => _onBack;
        ISubject<Unit> _onBack = new Subject<Unit>();

        [SerializeField] Transform _memberBtnList;

        Sprite[] _memberIconList = new Sprite[0];

        [SerializeField] GameObject _btnBack;

        [SerializeField] Material _iconMemberDefaultMat;
        [SerializeField] Material _iconMemberSelectedMat;

        IPlayerComponentCollection _playerOwner;
        int _curMemberIndexCache = 0;
        bool _inOperation = false;

        int _selectedIndex = -1;
        int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex >= 0) UpdateBtnSelectedStatus(_selectedIndex, false);
                _selectedIndex = value;
                if (_selectedIndex >= 0)
                {
                    UpdateBtnSelectedStatus(_selectedIndex, true);
                    _playerOwner.MemberController.ChangeMember(_selectedIndex);
                }
            }
        }

        void Start()
        {
            // 戻るボタン押したらSkill選択へ
            // 戻るボタン押したらSelect状態を解除し非表示
            _btnBack.OnClickListener(() =>
            {
                if (!_inOperation) return;

                _playerOwner.MemberController.ChangeMember(_curMemberIndexCache);
                CancelSelection();
                _onBack.OnNext(Unit.Default);
            }, gameObject);
        }

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                _playerOwner = playerOwner;
                var memberList = _playerOwner.MemberObservable.MemberList;
                _memberIconList = memberList.Select(member => member.ProfileSetting.Icon).ToArray();

                UpdateMemberIcon();

                // 各ボタンタップイベント登録
                SetupMemberSelectEvent();

                _playerOwner.CharacterInput.OnFire
                    .Where(_ => SelectedIndex >= 0)
                    .Subscribe(_ =>
                    {
                        CancelSelection();
                        _onBack.OnNext(Unit.Default);
                    })
                    .AddTo(this);

                SelectedIndex = _playerOwner.MemberObservable.CurMemberIndex;
            }
        }

        /*
        void ICharacterUI.SwitchOperation(bool inOperation)
        {
            _inOperation = inOperation;
            _curMemberIndexCache = _playerOwner.MemberObservable.CurMemberIndex;
            SelectedIndex = _curMemberIndexCache;
        }
        */

        void UpdateMemberIcon()
        {
            for (int i = 0; i < _memberBtnList.childCount; i++)
            {
                if (i > _memberIconList.Length - 1)
                {
                    _memberBtnList.GetChild(i).gameObject.SetActive(false);
                }
                else
                {
                    var optionBtn = _memberBtnList.GetChild(i);
                    Image icon = optionBtn.GetChild(1).GetComponent<Image>();
                    icon.sprite = _memberIconList[i];

                    _memberBtnList.GetChild(i).gameObject.SetActive(true);
                }
            }
        }

        void SetupMemberSelectEvent()
        {
            void SetUpMemberBtnEvent(Transform btn, int index)
            {
                btn.gameObject.OnClickListener(() =>
                {
                    // indexがMemberの数を超えていたらそもそもボタンが表示されないためタップできない
                    SelectedIndex = index;
                }, gameObject);
            }
            for (int i = 0; i < _memberBtnList.childCount; i++)
            {
#nullable enable
                Transform? detectButton = _memberBtnList.GetChild(i).GetChild(4);
                if (detectButton == null) continue;
#nullable disable
                SetUpMemberBtnEvent(detectButton, i);
            }
        }

        void UpdateBtnSelectedStatus(int index, bool isSelected)
        {
            if (isSelected)
            {
                _memberBtnList.GetChild(index).GetChild(0).GetComponent<Image>().material = _iconMemberSelectedMat;
            }
            else
            {
                _memberBtnList.GetChild(index).GetChild(0).GetComponent<Image>().material = _iconMemberDefaultMat;
            }
        }

        void CancelSelection()
        {
            if (SelectedIndex >= 0)
            {
                UpdateBtnSelectedStatus(SelectedIndex, false);
                SelectedIndex = -1;
            }
        }
    }
}
