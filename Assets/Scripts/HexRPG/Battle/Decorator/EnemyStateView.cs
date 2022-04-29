using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public class EnemyStateView : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [SerializeField] Text _aliveEnemyList;

        [Inject]
        public void Construct(IBattleObservable battleObservable)
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            _battleObservable.EnemyList.ObserveCountChanged()
                .Subscribe(_ =>
                {
                    var aliveEnemyList = "AliveEnemyList: ";
                    var enemyList = _battleObservable.EnemyList;
                    for(int i = 0; i < enemyList.Count; i++)
                    {
                        aliveEnemyList += enemyList[i].ProfileSetting.Name;
                        if (i < enemyList.Count - 1) aliveEnemyList += ", ";
                    }
                    _aliveEnemyList.text = aliveEnemyList;
                })
                .AddTo(this);
        }
    }
}
