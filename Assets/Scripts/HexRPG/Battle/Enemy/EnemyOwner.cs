using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public interface IEnemyComponentCollection : ICharacterComponentCollection
    {

    }

    public class EnemyOwner : MonoBehaviour, IEnemyComponentCollection
    {

    }
}
