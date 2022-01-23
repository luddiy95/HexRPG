using System.Collections.Generic;
using UnityEngine;

namespace HexRPG.Battle.Manager
{
    using Battle.Stage;
    using Battle.Player;

    public class StageManager : MonoBehaviour
    {
        [SerializeField] StageView _stageView;
        int _stageRadius;

        [SerializeField] PlayerManager _playerManager;
        PlayerModel _playerModel;

        public void Init()
        {
            //_stageView.InitStage(_stageRadius);
            if (!_stageView.TryGetComponent<StagePresenter>(out var stagePresenter)) return;
            stagePresenter.Init(_playerManager.PlayerModel);
        }
    }
}
