using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Combat;

    public class MemberCombatExecuter : ICombatController, ICombatSpawnController, ICombatSpawnObservable
    {
        IMemberComponentCollection _memberOwner;
        CombatOwner.Factory _combatFactory;
        ICombatSetting _combatSetting;

        ICombatComponentCollection ICombatSpawnObservable.Combat => _combat;
        ICombatComponentCollection _combat;

        bool ICombatSpawnObservable.isCombatSpawned => _isCombatSpawned;
        bool _isCombatSpawned = false;

        public MemberCombatExecuter(
            IMemberComponentCollection memberOwner,
            CombatOwner.Factory combatFactory,
            ICombatSetting combatSetting
        )
        {
            _memberOwner = memberOwner;
            _combatFactory = combatFactory;
            _combatSetting = combatSetting;
        }

        void ICombatSpawnController.Spawn(IAttackApplicator attackApplicator)
        {
            _combat = _combatFactory.Create(_combatSetting.SpawnRoot, Vector3.zero);
            _combat.Combat.Init(_combatSetting.Timeline, attackApplicator, _memberOwner.AnimationController);
            _isCombatSpawned = true;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            _combat.Combat.Execute();
            return _combat;
        }
    }
}
