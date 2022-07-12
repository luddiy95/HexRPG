using UnityEngine;
using System;
using Zenject;

namespace HexRPG.Battle.Stage.Tower
{
    public interface ITowerComponentCollection : ICharacterComponentCollection
    {
        ITowerController TowerController { get; }
        ITowerObservable TowerObservable { get; }
        IEnemySpawnObservable EnemySpawnObservable { get; }
    }

    public class TowerOwner : MonoBehaviour, ITowerComponentCollection
    {
        [Inject] IProfileSetting ICharacterComponentCollection.ProfileSetting { get; }
        IDieObservable ICharacterComponentCollection.DieObservable => throw new NullReferenceException();
        [Inject] IHealth ICharacterComponentCollection.Health { get; }

        [Inject] ITransformController IBaseComponentCollection.TransformController { get; }
        [Inject] ITowerController ITowerComponentCollection.TowerController { get; }
        [Inject] ITowerObservable ITowerComponentCollection.TowerObservable { get; }
        [Inject] IEnemySpawnObservable ITowerComponentCollection.EnemySpawnObservable { get; }
    }
}
