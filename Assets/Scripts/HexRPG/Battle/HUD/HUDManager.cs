using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.HUD
{
    using Enemy.HUD;

    public class HUDManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("MemberのHUD実装オブジェクト")]
        [SerializeField] GameObject _memberListHUD;

        // EnemyHealth
        EnemyStatusHUD.Factory _enemyStatusFactory;
        [SerializeField] Transform _enemyStatusRoot;

        DamagedPanelParentHUD.Factory _damagedPanelFactory;
        [SerializeField] Transform _damagedPanelRoot;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            EnemyStatusHUD.Factory enemyStatusFactory,
            DamagedPanelParentHUD.Factory damagedPanelFactory
        )
        {
            _battleObservable = battleObservable;
            _enemyStatusFactory = enemyStatusFactory;
            _damagedPanelFactory = damagedPanelFactory;
        }

        void Start()
        {
            // MemberList
            var playerHUD = _memberListHUD.GetComponents<ICharacterHUD>();
            Array.ForEach(playerHUD, hud =>
            {
                _battleObservable.OnPlayerSpawn
                    .Subscribe(playerOwner => hud.Bind(playerOwner))
                    .AddTo(this);
            });

            // EnemyStatus
            _battleObservable.OnEnemySpawn
                .Subscribe(enemyOwner =>
                {
                    var clone = _enemyStatusFactory.Create();
                    clone.transform.SetParent(_enemyStatusRoot);
                    var huds = clone.GetComponents<ICharacterHUD>();
                    Array.ForEach(huds, hud => hud.Bind(enemyOwner));
                })
                .AddTo(this);

            // DamagedPanel
            void SpawnDamagedPanel(ICharacterComponentCollection chara)
            {
                var clone = _damagedPanelFactory.Create();
                clone.transform.SetParent(_damagedPanelRoot);
                var huds = clone.GetComponents<ICharacterHUD>();
                Array.ForEach(huds, hud => hud.Bind(chara));
            }
            _battleObservable.OnPlayerSpawn
                .Subscribe(playerOwner => SpawnDamagedPanel(playerOwner))
                .AddTo(this);
            _battleObservable.OnEnemySpawn
                .Subscribe(enemyOwner => SpawnDamagedPanel(enemyOwner))
                .AddTo(this);
        }
    }
}
