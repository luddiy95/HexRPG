using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.HUD
{
    using Stage.Tower.HUD;
    using Enemy.HUD;

    public class HUDManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("MemberのHUD実装オブジェクト")]
        [SerializeField] GameObject _memberListHUD;

        [Header("AppendSkillのHUD実装オブジェクト")]
        [SerializeField] GameObject _appendSkillHUD;

        [Header("TowerStatusHUDのRoot")]
        [SerializeField] Transform _towerStatusRoot;

        EnemyStatusHUD.Factory _enemyStatusFactory;
        TowerStatusHUD.Factory _towerStatusFactory;
        DamagedPanelParentHUD.Factory _damagedPanelFactory;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            EnemyStatusHUD.Factory enemyStatusFactory,
            TowerStatusHUD.Factory towerStatusFactory,
            DamagedPanelParentHUD.Factory damagedPanelFactory
        )
        {
            _battleObservable = battleObservable;
            _enemyStatusFactory = enemyStatusFactory;
            _towerStatusFactory = towerStatusFactory;
            _damagedPanelFactory = damagedPanelFactory;
        }

        void Start()
        {
            // MemberList
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .First()
                .Subscribe(playerOwner => {
                    var huds = _memberListHUD.GetComponents<ICharacterHUD>();
                    foreach (var hud in huds) hud.Bind(playerOwner);
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
                    foreach (var hud in huds) hud.Bind(enemyOwner);
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
                foreach (var hud in huds) hud.Bind(chara);
            }
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .First()
                .Subscribe(playerOwner =>
                {
                    SpawnDamagedPanel(playerOwner);
                })
                .AddTo(this);
            _battleObservable.OnEnemySpawn
                .Subscribe(enemyOwner => SpawnDamagedPanel(enemyOwner))
                .AddTo(this);

            // AppendSkill
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .First()
                .Subscribe(playerOwner =>
                {
                    var huds = _appendSkillHUD.GetComponents<ICharacterHUD>();
                    foreach (var hud in huds) hud.Bind(playerOwner);
                })
                .AddTo(this);

            // Tower
            _battleObservable.OnTowerInit
                .Subscribe(towerOwner =>
                {
                    var towerStatusHUD = _towerStatusFactory.Create();
                    towerStatusHUD.transform.SetParent(_towerStatusRoot, false);

                    var huds = towerStatusHUD.GetComponents<ICharacterHUD>();
                    foreach (var hud in huds) hud.Bind(towerOwner);
                })
                .AddTo(this);
        }
    }
}
