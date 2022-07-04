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

        EnemyStatusHUD.Factory _enemyStatusFactory;

        DamagedPanelParentHUD.Factory _damagedPanelFactory;

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
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner => {
                    var playerHUD = _memberListHUD.GetComponents<ICharacterHUD>();
                    Array.ForEach(playerHUD, hud => hud.Bind(playerOwner));
                })
                .AddTo(this);

            // EnemyStatus
            _battleObservable.OnEnemySpawn
                .Subscribe(enemyOwner =>
                {
                    var enemyStatusHUD = _enemyStatusFactory.Create();

                    enemyOwner.DieObservable.IsDead
                        .Where(isDead => isDead)
                        .First()
                        .Subscribe(_ =>
                        {
                            enemyStatusHUD.Dispose();
                        })
                        .AddTo(this);

                    var huds = enemyStatusHUD.GetComponents<ICharacterHUD>();
                    Array.ForEach(huds, hud => hud.Bind(enemyOwner));
                })
                .AddTo(this);

            // DamagedPanel
            void SpawnDamagedPanel(ICharacterComponentCollection chara)
            {
                var damagedPanelParentHUD = _damagedPanelFactory.Create();

                chara.DieObservable.OnFinishDie
                    .First()
                    .Subscribe(_ => damagedPanelParentHUD.Dispose())
                    .AddTo(this);

                var huds = damagedPanelParentHUD.GetComponents<ICharacterHUD>();
                Array.ForEach(huds, hud => hud.Bind(chara));
            }
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    SpawnDamagedPanel(playerOwner);
                })
                .AddTo(this);
            _battleObservable.OnEnemySpawn
                .Subscribe(enemyOwner => SpawnDamagedPanel(enemyOwner))
                .AddTo(this);
        }
    }
}
