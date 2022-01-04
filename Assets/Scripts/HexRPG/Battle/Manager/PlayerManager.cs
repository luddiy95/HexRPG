using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Manager
{
    using Stage;
    using Player;
    using Player.Character;
    using Player.Panel;

    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] PlayerView _playerPrefab;
        PlayerView _playerView;
        public PlayerModel PlayerModel { get; private set; }

        [SerializeField] List<CharacterData> _characterDataList = new List<CharacterData>();

        [SerializeField] SelectSkillPanelPresenter _selectSkillPanelPresenter;
        [SerializeField] SelectCharacterPanelPresenter _selectCharacterPanelPresenter;
        [SerializeField] StatusPanelPresenter _statusPanelPresenter;

        public void RespawnPlayer()
        {
            _playerView = Instantiate(_playerPrefab);
            if (!_playerView.TryGetComponent<PlayerPresenter>(out var playerPresenter)) return;
            playerPresenter.Init(_characterDataList);

            PlayerModel = playerPresenter.Model;

            _selectSkillPanelPresenter.Init(PlayerModel);
            _selectCharacterPanelPresenter.Init(PlayerModel);
            _statusPanelPresenter.Init(PlayerModel);
        }

        public Hex PlayerLandedHex => _playerView.LandedHex;
    }
}
