using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Player.Panel
{
    [RequireComponent(typeof(SelectCharacterPanelView))]
    public class SelectCharacterPanelPresenter : MonoBehaviour
    {
        SelectCharacterPanelView _view;
        PlayerModel _playerModel;

        public void Init(PlayerModel playerModel)
        {
            _playerModel = playerModel;
        }
    }
}
