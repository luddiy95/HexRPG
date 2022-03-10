using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle
{
    public class HUDManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("Player��HUD�����I�u�W�F�N�g")]
        [SerializeField] GameObject _playerHUD;

        [Header("Enemy��HUD�����I�u�W�F�N�g")]
        [SerializeField] GameObject _enemyHUD;

        [Inject]
        public void Construct(IBattleObservable battleObservable)
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            var playerHUD = _playerHUD.GetComponents<ICharacterHUD>();
            Array.ForEach(playerHUD, hud =>
            {
                _battleObservable.OnPlayerSpawn
                    .Skip(1)
                    .Subscribe(playerOwner => hud.Bind(playerOwner))
                    .AddTo(this);
            });

            var enemyHUD = _enemyHUD.GetComponents<ICharacterHUD>();
            Array.ForEach(enemyHUD, hud =>
            {
                _battleObservable.OnEnemySpawn
                    .Subscribe(playerOwner => hud.Bind(playerOwner))
                    .AddTo(this);
            });
        }
    }
}
