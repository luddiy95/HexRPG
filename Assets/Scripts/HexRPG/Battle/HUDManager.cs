using UnityEngine;
using UniRx;

namespace HexRPG.Battle
{
    public class HUDManager : AbstractCustomComponentBehaviour
    {
        [Header("Player��HUD�����I�u�W�F�N�g")]
        [SerializeField] GameObject _playerHUD;

        [Header("Enemy��HUD�����I�u�W�F�N�g")]
        [SerializeField] GameObject _enemyHUD;

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.QueryInterface(out IComponentCollectionFactory factory)) return;

            var playerHUD = factory.CreateComponentCollectionWithoutInstantiate(_playerHUD, null, null);
            var enemyHUD = factory.CreateComponentCollectionWithoutInstantiate(_enemyHUD, null, null);

            if (Owner.QueryInterface(out IBattleObservable battleObservable))
            {
                if (playerHUD.QueryInterface(out ICharacterHUD phud))
                {
                    battleObservable.OnPlayerSpawn
                        .Subscribe(player => phud.Bind(player))
                        .AddTo(this);
                }

                if(enemyHUD.QueryInterface(out ICharacterHUD ehud))
                {
                    battleObservable.OnEnemySpawn
                        .Subscribe(enemy => ehud.Bind(enemy))
                        .AddTo(this);
                }
            }
        }
    }
}
