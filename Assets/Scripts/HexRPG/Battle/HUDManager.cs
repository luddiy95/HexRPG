using UnityEngine;
using UniRx;
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
            var playerHUD = _playerHUD.GetComponent<ICharacterHUD>();

            _battleObservable.OnPlayerSpawn
                .Subscribe(playerOwner => playerHUD.Bind(playerOwner))
                .AddTo(this);

            //TODO:
            /*
            battleObservable.OnEnemySpawn
                .Subscribe(enemy => ehud.Bind(enemy))
                .AddTo(this);
            */
        }
    }
}
