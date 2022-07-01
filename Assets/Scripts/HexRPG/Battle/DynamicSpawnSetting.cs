using UnityEngine;

namespace HexRPG.Battle
{
    [System.Serializable]
    public class DynamicSpawnSetting : SpawnSetting
    {
        public int MaxCount => _maxCount;
        [SerializeField] int _maxCount;

        public int FirstSpawnInterval => _firstSpawnInterval;
        [SerializeField] int _firstSpawnInterval;

        public int SpawnInterval => _spawnInterval;
        [SerializeField] int _spawnInterval;
    }
}
