using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player.Combat
{
    public interface ICombatComponentCollection
    {
        ICombat Combat { get; }
        ICombatObservable CombatObservable { get; }
    }

    public class CombatOwner : MonoBehaviour, ICombatComponentCollection
    {
        [Inject] ICombat ICombatComponentCollection.Combat { get; }
        [Inject] ICombatObservable ICombatComponentCollection.CombatObservable { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, CombatOwner>
        {

        }
    }
}
