using UnityEngine;
using System;
using UnityEngine.Playables;

namespace HexRPG.Battle
{
    public interface ISkillsSetting
    {
        SkillAsset[] Skills { get; }
    }

    [Serializable]
    public class SkillAsset
    {
        public GameObject Prefab => _prefab;
        [SerializeField] GameObject _prefab;

        public PlayableAsset Timeline => _timeline;
        [SerializeField] PlayableAsset _timeline;
    }
}
