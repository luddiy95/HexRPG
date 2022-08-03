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
        IAttackComponentCollection _attackOwner;
        List<CombatOwner.Factory> _combatFactories;
        ICombatEquipment _combatEquipment;

        IObservable<Unit> ICombatObservable.OnFinishCombat => _onFinishCombat;
        readonly ISubject<Unit> _onFinishCombat = new Subject<Unit>();

        ICombatComponentCollection ICombatSpawnObservable.Combat => _combat;
        ICombatComponentCollection _combat;

        bool ICombatSpawnObservable.IsCombatSpawned => _isCombatSpawned;
        bool _isCombatSpawned = false;

        CompositeDisposable _disposables = new CompositeDisposable();

        public EnemyCombatExecuter(
            IAttackComponentCollection attackOwner,
            List<CombatOwner.Factory> combatFactories,
            ICombatEquipment combatEquipment
        )
        {
            _attackOwner = attackOwner;
            _combatFactories = combatFactories;
            _combatEquipment = combatEquipment;
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
            // I—¹ˆ—
            _combat.CombatObservable.OnFinishCombat
                .Subscribe(_ =>
                {
                    _disposables.Clear();

                    _onFinishCombat.OnNext(Unit.Default);
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
