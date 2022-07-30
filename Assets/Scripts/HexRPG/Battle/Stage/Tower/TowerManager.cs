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

        Hex TowerCenter { get; }
        Hex[] FixedHexList { get; }
    }

    public class TowerManager : MonoBehaviour, ITowerController, ITowerObservable
    {
        IReadOnlyReactiveProperty<TowerType> ITowerObservable.TowerType => _towerType;
        IReactiveProperty<TowerType> _towerType = new ReactiveProperty<TowerType>();

        [SerializeField] TowerType _initTowerType;

        Hex ITowerObservable.TowerCenter => _towerCenter;
        [SerializeField] Hex _towerCenter;

        Hex[] ITowerObservable.FixedHexList => _fixedHexList;
        [SerializeField] Hex[] _fixedHexList;

        IHealth _health;

        [SerializeField] GameObject _enemyCrystal;
        [SerializeField] GameObject _playerCrystal;

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
            await UniTask.Yield(token); // Tower��Health.Current = 0��HealthGaugeHUD�ɔ��f����Ă���
            _towerType.Value = (_towerType.Value == TowerType.PLAYER) ? TowerType.ENEMY : TowerType.PLAYER;
        }

        //TODO: TowerType==ENEMY�̏ꍇ�̂�EnemyManager�����삷��
        //TODO: TowerType==PLAYER�ɂȂ�����EnemyManager��Spawn�Ȃǂ͒�~
        //TODO: TowerType==PLAYER����ENEMY�ɂȂ�����H -> Tower�̎���(Player�������ꍇ��PlayerLandedHex�ȊO)��EnemyHex�ɂ���Spawn�ĊJ

        //TODO: TowerType==PLAYER�̂Ƃ��ATower���ӂ�hex�ɂ����Player��HP��SP���񕜂���
    }
}
