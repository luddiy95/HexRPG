using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Enemy
{
    using Combat;

    public class EnemyCombatExecuter : ICombatSpawnObservable, ICombatController, ICombatObservable, IInitializable, IDisposable
    {
        IUpdateObservable _updateObservable;
        IAttackComponentCollection _attackOwner;
        List<CombatOwner.Factory> _combatFactories;
        ICombatEquipment _combatEquipment;
        ILocomotionController _locomotionController;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection ICombatSpawnObservable.Combat => _combat;
        ICombatComponentCollection _combat;

        bool ICombatSpawnObservable.IsCombatSpawned => _isCombatSpawned;
        bool _isCombatSpawned = false;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyCombatExecuter(
            IUpdateObservable updateObservable,
            IAttackComponentCollection attackOwner,
            List<CombatOwner.Factory> combatFactories,
            ICombatEquipment combatEquipment,
            ILocomotionController locomotionController
        )
        {
            _updateObservable = updateObservable;
            _attackOwner = attackOwner;
            _combatFactories = combatFactories;
            _combatEquipment = combatEquipment;
            _locomotionController = locomotionController;
        }

        void IInitializable.Initialize()
        {
            if (_combatFactories.Count == 0)
            {
                _isCombatSpawned = true;
                return;
            }

            if (_attackOwner is IEnemyComponentCollection enemyOwner)
            {
                _combat = _combatFactories[0].Create(enemyOwner.TransformController.SpawnRootTransform("Combat"), Vector3.zero);

                _combat.Combat.Init(_attackOwner, enemyOwner.AnimationController, _combatEquipment.Timeline);
                if (_combatEquipment.CombatType == CombatType.PROXIMITY) _combat.Combat.AttackColliderRoot = _combatEquipment.EquipmentRoot;

                _isCombatSpawned = true;
            }
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            _disposables.Clear();
            // 終了処理
            _combat.CombatObservable.OnFinishCombat
                .Subscribe(_ =>
                {
                    _locomotionController.Stop();
                    _disposables.Clear();

                    _onFinishCombat.OnNext(Unit.Default);
                })
                .AddTo(_disposables);
            // Velocity更新
            _updateObservable
                .OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    var velocity = _combat.Combat.Velocity;
                    _locomotionController.SetSpeed(velocity, velocity.magnitude);
                })
                .AddTo(_disposables);
            _combat.Combat.Execute();
            return _combat;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
