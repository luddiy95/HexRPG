using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Manager
{
    using Stage;
    using Player;
    using Player.Member;
    using Player.Panel;

    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] PlayerView _playerPrefab;
        PlayerView _playerView;
        public PlayerModel PlayerModel { get; private set; }

        [SerializeField] List<MemberData> _memberDataList = new List<MemberData>();

        [SerializeField] SelectSkillPanelPresenter _selectSkillPanelPresenter;
        [SerializeField] SelectCharacterPanelPresenter _selectCharacterPanelPresenter;
        [SerializeField] StatusPanelPresenter _statusPanelPresenter;

        public void RespawnPlayer()
        {
            _playerView = Instantiate(_playerPrefab);
            if (!_playerView.TryGetComponent<PlayerPresenter>(out var playerPresenter)) return;
            playerPresenter.Init(_memberDataList);

            PlayerModel = playerPresenter.Model;

            _selectSkillPanelPresenter.Init(PlayerModel);
            _selectCharacterPanelPresenter.Init(PlayerModel);
            _statusPanelPresenter.Init(PlayerModel);
        }

        public Hex PlayerLandedHex => _playerView.LandedHex;
    }
}
