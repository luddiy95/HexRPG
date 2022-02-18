using UnityEngine;
using UniRx;
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
