using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UniRx;
using Zenject;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

namespace HexRPG.Battle.Stage.Tower
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

        List<Hex> EnemyHexList { get; }
        Hex TowerCenter { get; }
        Hex[] FixedHexList { get; }
    }

    public class TowerManager : MonoBehaviour, ITowerController, ITowerObservable
    {
        IReadOnlyReactiveProperty<TowerType> ITowerObservable.TowerType => _towerType;
        IReactiveProperty<TowerType> _towerType = new ReactiveProperty<TowerType>();

        [SerializeField] TowerType _initTowerType;

        List<Hex> ITowerObservable.EnemyHexList
        {
            get
            {
                _enemyHexList.Clear();
                for (int i = 0; i < _hexRoot.childCount; i++)
                {
                    var hex = _hexRoot.GetChild(i).GetComponent<Hex>();
                    if (hex == null) continue;
                    if (hex.IsPlayerHex == false) _enemyHexList.Add(hex);
                }
                return _enemyHexList;
            }
        }
        List<Hex> _enemyHexList = new List<Hex>(256);
        Hex ITowerObservable.TowerCenter => _towerCenter;
        [SerializeField] Hex _towerCenter;
        Hex[] ITowerObservable.FixedHexList => _fixedHexList;
        [SerializeField] Hex[] _fixedHexList;

        IHealth _health;

        [SerializeField] GameObject _enemyCrystal;
        [SerializeField] GameObject _playerCrystal;

        [SerializeField] Transform _hexRoot;

        [Inject]
        public void Construct(
            IHealth health
        )
        {
            _health = health;
        }

        void ITowerController.Init()
        {
            _towerType.Value = _initTowerType;
            _towerType
                .Subscribe(type =>
                {
                    var isTowerTypePlayer = type == TowerType.PLAYER;
                    _playerCrystal.SetActive(isTowerTypePlayer);
                    _enemyCrystal.SetActive(!isTowerTypePlayer);
                    foreach (var fixedHex in _fixedHexList) fixedHex.UpdateFixedHexStatus(isTowerTypePlayer);

                    _health.Init();
                })
                .AddTo(this);

            _health.Current
                .Where(health => health <= 0)
                .Subscribe(health =>
                {
                    SwitchTowerType(this.GetCancellationTokenOnDestroy()).Forget();
                })
                .AddTo(this);
        }

        async UniTaskVoid SwitchTowerType(CancellationToken token)
        {
            await UniTask.Yield(token); // TowerのHealth.Current = 0がHealthGaugeHUDに反映されてから
            _towerType.Value = (_towerType.Value == TowerType.PLAYER) ? TowerType.ENEMY : TowerType.PLAYER;
        }

        //TODO: TowerType==ENEMYの場合のみEnemyManagerが動作する
        //TODO: TowerType==PLAYERになったらEnemyManagerのSpawnなどは停止
        //TODO: TowerType==PLAYERからENEMYになったら？ -> Towerの周囲(Playerがいた場合はPlayerLandedHex以外)をEnemyHexにしてSpawn再開

        //TODO: TowerType==PLAYERのとき、Tower周辺のhexにいるとPlayerのHPやSPが回復する
    }
}
