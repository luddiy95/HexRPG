using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using UniRx;

namespace HexRPG.Battle
{
    public class PlayerStateView : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [SerializeField] Text _playerStateText;

        [Inject]
        public void Construct(IBattleObservable battleObservable)
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    playerOwner.ActionStateObservable.CurrentState
                        .Where(state => state != null)
                        .Subscribe(state =>
                        {
                            _playerStateText.text = "PlayerState: " + state.Type.ToString();
                        }).AddTo(this);
                }).AddTo(this);
        }

        void Update()
        {

        }
    }
}
