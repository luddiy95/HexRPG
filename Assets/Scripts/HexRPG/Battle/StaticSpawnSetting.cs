using UnityEngine;

namespace HexRPG.Battle
{
    using Stage;

    [System.Serializable]
    public class StaticSpawnSetting : SpawnSetting
    {
        public Hex SpawnHex => _spawnHex;
        [SerializeField] Hex _spawnHex;
    }
}
