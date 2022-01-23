using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Player.Panel
{
    public class StatusPanelPresenter : MonoBehaviour
    {
        StatusPanelView _view;
        PlayerModel _playerModel;

        public void Init(PlayerModel playerModel)
        {
            _view = GetComponent<StatusPanelView>();
            _playerModel = playerModel;

            SubscribeCharacterChange();
            SubscribeCharacterUpdate();
        }

        void SubscribeCharacterChange()
        {
            _playerModel
                .CurMember
                .Subscribe(character =>
                {
                    _view.SetStatusIcon(character.StatusIcon);
                    _view.SetHP(character.MaxHP, character.HP);
                    _view.SetMP(character.MaxMP, character.MP);
                })
                .AddTo(this);
        }

        void SubscribeCharacterUpdate()
        {
            _playerModel
                .CurMemberHP
                .Skip(1)
                .Subscribe(amount => _view.UpdateHP(amount))
                .AddTo(this);

            _playerModel
                .CurMemberMP
                .Skip(1)
                .Subscribe(amount => _view.UpdateMP(amount))
                .AddTo(this);
        }
    }
}
