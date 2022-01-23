using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Player.Panel
{
    [RequireComponent(typeof(SelectCharacterPanelView))]
    public class SelectCharacterPanelPresenter : SelectBasePanelPresenter
    {
        public override void Init(PlayerModel playerModel)
        {
            base.Init(playerModel);

            List<Sprite> spriteList = new List<Sprite>();
            _playerModel.MemberList.ForEach(character => spriteList.Add(character.Icon));
            if (_view is SelectCharacterPanelView selectCharacterPanelView)
                selectCharacterPanelView.InitCharacterBtnList(spriteList);
        }

        protected override void OnCharacterChanged(Member.Member member)
        {

        }

        protected override void OnOptionBtnSelected(int index)
        {
            _playerModel.TryUpdateSelectedMemberIndex(index);
        }

        protected override void SubscribeSelectPanel()
        {
            _playerModel
                .CurSelectedMemberIndex
                .Skip(1)
                .Subscribe(index => {
                    _view.SetOptionBtnSelectedStatus(index, true);
                    })
                .AddTo(this);

            _playerModel
                .ClearSelectedMemberIndex
                .Subscribe(_ =>
                {
                    if (_playerModel.CurSelectedMemberIndex.Value == -1) return;
                    _view.SetOptionBtnSelectedStatus(_playerModel.CurSelectedMemberIndex.Value, false);
                })
                .AddTo(this);

            _playerModel
                .CloseSelectMemberPanel
                .Subscribe(_ => _view.CloseSelectPanel())
                .AddTo(this);

            _playerModel
                .OpenSelectMemberPanel
                .Subscribe(_ => _view.OpenSelectPanel())
                .AddTo(this);
        }
    }
}
