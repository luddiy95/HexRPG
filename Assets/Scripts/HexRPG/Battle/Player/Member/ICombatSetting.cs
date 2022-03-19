using UnityEngine;
using System;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Member
{
    public interface ICombatSetting
    {
        CombatAsset Combat { get; }
    }

    [Serializable]
    public class CombatAsset
    {
        public GameObject Prefab => _prefab;
        [SerializeField] GameObject _prefab;

        public PlayableAsset Timeline => _timeline;
        [SerializeField] PlayableAsset _timeline;
    }
}
