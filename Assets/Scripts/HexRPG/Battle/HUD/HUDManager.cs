using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class HUDManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("Member��HUD�����I�u�W�F�N�g")]
        [SerializeField] GameObject _memberList;

        [Header("Enemy��HUD�����I�u�W�F�N�g")]
        [SerializeField] GameObject _enemyStatus;

        [Inject]
        public void Construct(IBattleObservable battleObservable)
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            var playerHUD = _memberList.GetComponents<ICharacterHUD>();
            Array.ForEach(playerHUD, hud =>
            {
                _battleObservable.OnPlayerSpawn
                    .Subscribe(playerOwner => hud.Bind(playerOwner))
                    .AddTo(this);
            });

            var enemyHUD = _enemyStatus.GetComponents<ICharacterHUD>();
            Array.ForEach(enemyHUD, hud =>
            {
                _battleObservable.OnEnemySpawn
                    .Subscribe(enemyOwner => hud.Bind(enemyOwner))
                    .AddTo(this);
            });
        }
    }
}
