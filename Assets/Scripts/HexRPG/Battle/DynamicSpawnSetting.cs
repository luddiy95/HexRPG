using UnityEngine;

namespace HexRPG.Battle
{
    [System.Serializable]
    public class DynamicSpawnSetting : SpawnSetting
    {
        public int MaxCount => _maxCount;
        [SerializeField] int _maxCount;

        public int FirstSpawnInterval => _firstSpawnInterval;
        [Header("�ŏ���Spawn��Interval(ms)")]
        [SerializeField] int _firstSpawnInterval;

        public int SpawnInterval => _spawnInterval;
        [Header("2��ڈȍ~��Spawn��Interval(ms)")]
        [SerializeField] int _spawnInterval;
    }
}
