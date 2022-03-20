using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Member
{
    public interface ICombatSetting
    {
        GameObject Prefab { get; }
        Transform SpawnRoot { get; }
        PlayableAsset Timeline { get; }
    }
}
