using UnityEngine;
using System;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISpawnSettings : IFeature
    {
        SpawnSetting PlayerSpawnSetting { get; }
        SpawnSetting[] EnemySpawnSettings { get; }
    }

    [Serializable]
    public class SpawnSetting
    {
        public GameObject Prefab => _prefab;
        [SerializeField] GameObject _prefab;

        public Hex SpawnHex => _spawnHex;
        [SerializeField] Hex _spawnHex;
    }
}
