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
                        //TODO: TowerType==PLAYER����ENEMY�ɂȂ�����H -> Tower�̎���(Player�������ꍇ��PlayerLandedHex�ȊO)��EnemyHex�ɂ���Spawn�ĊJ
                    }
                })
                .AddTo(this);

            _towerType.Value = _initTowerType;
        }

        //TODO: TowerType==ENEMY�̏ꍇ�̂�EnemyManager�����삷��
        //TODO: TowerType==PLAYER�ɂȂ�����EnemyManager��Spawn�Ȃǂ͒�~
        //TODO: TowerType==PLAYER����ENEMY�ɂȂ�����H -> Tower�̎���(Player�������ꍇ��PlayerLandedHex�ȊO)��EnemyHex�ɂ���Spawn�ĊJ

        //TODO: TowerType==PLAYER�̂Ƃ��ATower���ӂ�hex�ɂ����Player��HP��SP���񕜂���
    }
}
