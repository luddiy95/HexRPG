using UnityEngine;

namespace HexRPG.Battle
{
    [System.Serializable]
    public class DynamicSpawnSetting : SpawnSetting
    {
        public int MaxCount => _maxCount;
        [SerializeField] int _maxCount;

        public int FirstSpawnCount => _firstSpawnCount;
        [Header("Å‰‚ÉSpawn‚·‚é”i >= 1j")]
        [SerializeField] int _firstSpawnCount;

        public int FirstSpawnInterval => _firstSpawnInterval;
        [Header("Å‰‚ÌSpawn‚ÌInterval(ms)")]
        [SerializeField] int _firstSpawnInterval;

        public int SpawnInterval => _spawnInterval;
        [Header("2‰ñ–ÚˆÈ~‚ÌSpawn‚ÌInterval(ms)")]
        [SerializeField] int _spawnInterval;
    }
}
