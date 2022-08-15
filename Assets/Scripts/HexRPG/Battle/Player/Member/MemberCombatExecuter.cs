using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Combat;

    public class MemberCombatExecuter : ICombatController, ICombatSpawnController, ICombatSpawnObservable
    {
        ITransformController _transformController;
        IAnimationController _animationController;
        CombatOwner.Factory _combatFactory;
        ICombatEquipment _combatEquipment;

        ICombatComponentCollection ICombatSpawnObservable.Combat => _combat;
        ICombatComponentCollection _combat;

        bool ICombatSpawnObservable.IsCombatSpawned => _isCombatSpawned;
        bool _isCombatSpawned = false;

        public MemberCombatExecuter(
            ITransformController transformController,
            IAnimationController animationController,
            CombatOwner.Factory combatFactory,
            ICombatEquipment combatEquipment
        )
        {
            _transformController = transformController;
            _animationController = animationController;
            _combatFactory = combatFactory;
            _combatEquipment = combatEquipment;
        }

        void ICombatSpawnController.Spawn(IAttackComponentCollection attackOwner)
        {
            var equipment = Object.Instantiate(_combatEquipment.EquipmentPrefab, _combatEquipment.EquipmentRoot);

            _combat = _combatFactory.Create(_transformController.SpawnRootTransform("Combat"), Vector3.zero);

            _combat.Combat.Init(attackOwner, _animationController, _combatEquipment.Timeline);
            //if (_combatEquipment.CombatType == CombatType.PROXIMITY) _combat.Combat.AttackColliderRoot = equipment.transform;

            _isCombatSpawned = true;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            _combat.Combat.Execute();
            return _combat;
        }
    }
}
