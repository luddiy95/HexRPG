using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Combat
{
    public interface ICombatComponentCollection
    {
        ICombat Combat { get; }
        ICombatSetting CombatSetting { get; }
        ICombatObservable CombatObservable { get; }
    }

    public class CombatOwner : MonoBehaviour, ICombatComponentCollection
    {
        [Inject] ICombat ICombatComponentCollection.Combat { get; }
        [Inject] ICombatSetting ICombatComponentCollection.CombatSetting { get; }
        [Inject] ICombatObservable ICombatComponentCollection.CombatObservable { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, CombatOwner>
        {

        }
    }
}
