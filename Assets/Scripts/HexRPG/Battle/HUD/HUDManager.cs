using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public class HUDManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("MemberのHUD実装オブジェクト")]
        [SerializeField] GameObject _memberListHUD;

        // EnemyHealth
        HealthGaugeHUD.Factory _healthGaugeFactory;
        [SerializeField] Transform _enemyHealthGaugeRoot;

        DamagedPanelParentHUD.Factory _damagedPanelFactory;
        [SerializeField] Transform _damagedPanelRoot;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            HealthGaugeHUD.Factory healthGaugeFactory,
            DamagedPanelParentHUD.Factory damagedPanelFactory
        )
        {
            _battleObservable = battleObservable;
            _healthGaugeFactory = healthGaugeFactory;
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

            // EnemyHealthGauge
            _battleObservable.OnEnemySpawn
                .Subscribe(enemyOwner =>
                {
                    var clone = _healthGaugeFactory.Create();
                    clone.transform.SetParent(_enemyHealthGaugeRoot);
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
