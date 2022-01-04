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

            SubscribeInitCharacters();
        }

        protected override void OnCharacterChanged(Character.Character character)
        {

        }

        protected override void OnOptionBtnSelected(int index)
        {
            _playerModel.ChangeCharacter(index);
        }

        void SubscribeInitCharacters()
        {
            _playerModel
                .InitCharacters
                .Subscribe(_ =>
                {
                    List<Sprite> spriteList = new List<Sprite>();
                    _playerModel.CharacterList.ForEach(character => spriteList.Add(character.Icon));
                    if (_view is SelectCharacterPanelView selectCharacterPanelView) 
                        selectCharacterPanelView.InitCharacterBtnList(spriteList);
                })
                .AddTo(this);
        }

        protected override void SubscribeSelectPanel()
        {
            _playerModel
                .CurSelectedCharacterIndex
                .Skip(1)
                .Subscribe(index => _view.SetOptionBtnSelectedStatus(index, true))
                .AddTo(this);

            _playerModel
                .ClearSelectedCharacterIndex
                .Subscribe(_ => _view.SetOptionBtnSelectedStatus(_playerModel.CurSelectedCharacterIndex.Value, false))
                .AddTo(this);

            _playerModel
                .CloseSelectCharacterPanel
                .Subscribe(_ => _view.CloseSelectPanel())
                .AddTo(this);

            _playerModel
                .OpenSelectCharacterPanel
                .Subscribe(_ => _view.OpenSelectPanel())
                .AddTo(this);
        }
    }
}
