using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UniRx;
using Zenject;
using System;

namespace HexRPG.Battle.Stage
{
    public enum TowerType
    {
        PLAYER,
        ENEMY
    }

    public interface ITowerController
    {
        void Init();
    }

    public interface ITowerObservable
    {
        IReadOnlyReactiveProperty<TowerType> TowerType { get; }
        Hex RootHex { get; }
        Hex[] FixedEnemyHexList { get; }
    }

    public class TowerManager : MonoBehaviour, ITowerController, ITowerObservable
    {
        IReadOnlyReactiveProperty<TowerType> ITowerObservable.TowerType => _towerType;
        IReactiveProperty<TowerType> _towerType = new ReactiveProperty<TowerType>();

        [SerializeField] TowerType _initTowerType;

        Hex ITowerObservable.RootHex => _rootHex;
        [SerializeField] Hex _rootHex;
        Hex[] ITowerObservable.FixedEnemyHexList => _fixedEnemyHexList;
        [SerializeField] Hex[] _fixedEnemyHexList;

        IHealth _health;

        [Inject]
        public void Construct(
            IHealth health
        )
        {
            _health = health;
        }

        void ITowerController.Init()
        {
            _towerType
                .Subscribe(type =>
                {
                    switch (type)
                    {
                        //TODO: TowerType==PLAYERからENEMYになったら？ -> Towerの周囲(Playerがいた場合はPlayerLandedHex以外)をEnemyHexにしてSpawn再開
                    }
                })
                .AddTo(this);
            _towerType.Value = _initTowerType;

            _health.Current
                .Subscribe(health =>
                {

                })
                .AddTo(this);
        }

        //TODO: TowerType==ENEMYの場合のみEnemyManagerが動作する
        //TODO: TowerType==PLAYERになったらEnemyManagerのSpawnなどは停止
        //TODO: TowerType==PLAYERからENEMYになったら？ -> Towerの周囲(Playerがいた場合はPlayerLandedHex以外)をEnemyHexにしてSpawn再開

        //TODO: TowerType==PLAYERのとき、Tower周辺のhexにいるとPlayerのHPやSPが回復する
    }
}
