using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace HexRPG.Battle.Manager
{
    public class GameManager : BaseManager
    {
        [SerializeField] StageManager _stageManager;
        [SerializeField] PlayerManager _playerManager;

        protected override void Init()
        {
            base.Init();

            _playerManager.RespawnPlayer();
            _stageManager.Init();
        }

        public override void GameUpdate()
        {
            base.GameUpdate();
        }
    }
}
