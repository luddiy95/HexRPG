using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    using Combat;

    public class MemberCombatExecuter : ICombatController, ICombatSpawnObservable, IInitializable
    {
        IMemberComponentCollection _memberOwner;
        CombatOwner.Factory _combatFactory;
        ICombatSetting _combatSetting;
        ITransformController _transformController;
        IAnimatorController _animatorController;

        ICombatComponentCollection ICombatSpawnObservable.Combat => _combat;
        ICombatComponentCollection _combat;

        bool ICombatSpawnObservable.isCombatSpawned => _isCombatSpawned;
        bool _isCombatSpawned = false;

        public MemberCombatExecuter(
            IMemberComponentCollection memberOwner,
            CombatOwner.Factory combatFactory,
            ICombatSetting combatSetting,
            ITransformController transformController,
            IAnimatorController animatorController
        )
        {
            _memberOwner = memberOwner;
            _combatFactory = combatFactory;
            _combatSetting = combatSetting;
            _transformController = transformController;
            _animatorController = animatorController;
        }

        void IInitializable.Initialize()
        {
            _combat = _combatFactory.Create(_transformController.SpawnRootTransform("Combat"), Vector3.zero);
            _combat.Combat.Init(_combatSetting.Combat.Timeline, _memberOwner, _animatorController.Animator);
            _isCombatSpawned = true;
        }

        ICombatComponentCollection ICombatController.Combat()
        {
            _combat.Combat.Execute();
            return _combat;
        }
    }
}
