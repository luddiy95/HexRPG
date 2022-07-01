using UnityEngine;
using Zenject;

namespace HexRPG.Battle
{
    using Enemy;

    public interface ITowerComponentCollection
    {
        ITowerController TowerController { get; }
        ITowerObservable TowerObservable { get; }
        IEnemySpawnObservable EnemySpawnObservable { get; }
    }

    public class TowerOwner : MonoBehaviour, ITowerComponentCollection
    {
        [Inject] ITowerController ITowerComponentCollection.TowerController { get; }
        [Inject] ITowerObservable ITowerComponentCollection.TowerObservable { get; }
        [Inject] IEnemySpawnObservable ITowerComponentCollection.EnemySpawnObservable { get; }
    }
}
