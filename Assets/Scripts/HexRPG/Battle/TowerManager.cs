using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UniRx;
using System;

namespace HexRPG.Battle
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
    }

    public class TowerManager : MonoBehaviour, ITowerController, ITowerObservable
    {
        IReadOnlyReactiveProperty<TowerType> ITowerObservable.TowerType => _towerType;
        IReactiveProperty<TowerType> _towerType = new ReactiveProperty<TowerType>();

        [SerializeField] TowerType _initTowerType;

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
        }

        //TODO: TowerType==ENEMYの場合のみEnemyManagerが動作する
        //TODO: TowerType==PLAYERになったらEnemyManagerのSpawnなどは停止
        //TODO: TowerType==PLAYERからENEMYになったら？ -> Towerの周囲(Playerがいた場合はPlayerLandedHex以外)をEnemyHexにしてSpawn再開

        //TODO: TowerType==PLAYERのとき、Tower周辺のhexにいるとPlayerのHPやSPが回復する
    }
}
