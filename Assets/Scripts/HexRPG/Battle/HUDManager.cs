using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle
{
    public class HUDManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("PlayerのHUD実装オブジェクト")]
        [SerializeField] GameObject _playerHUD;

        [Header("EnemyのHUD実装オブジェクト")]
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
