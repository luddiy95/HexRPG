using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Combat;

    public class MemberCombatExecuter : ICombatController, ICombatSpawnController, ICombatSpawnObservable
    {
        IAnimationController _animationController;
        CombatOwner.Factory _combatFactory;
        ICombatEquipment _combatEquipment;

        ICombatComponentCollection ICombatSpawnObservable.Combat => _combat;
        ICombatComponentCollection _combat;

        bool ICombatSpawnObservable.isCombatSpawned => _isCombatSpawned;
        bool _isCombatSpawned = false;

        public MemberCombatExecuter(
            IAnimationController animationController,
            CombatOwner.Factory combatFactory,
            ICombatEquipment combatEquipment
        )
        {
            _animationController = animationController;
            _combatFactory = combatFactory;
            _combatEquipment = combatEquipment;
        }

        void ICombatSpawnController.Spawn(IAttackComponentCollection attackOwner)
        {
            _combat = _combatFactory.Create(_combatEquipment.SpawnRoot, Vector3.zero);
            _combat.Combat.Init(attackOwner, _animationController, _combatEquipment.Timeline);
            _isCombatSpawned = true;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            _combat.Combat.Execute();
            return _combat;
        }
    }
}
