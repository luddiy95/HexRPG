using UnityEngine;

namespace HexRPG.Battle
{
    public class SpawnSetting
    {
        public GameObject Prefab => _prefab;
        [SerializeField] GameObject _prefab;
    }
}
