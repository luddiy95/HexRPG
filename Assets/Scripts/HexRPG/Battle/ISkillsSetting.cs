using UnityEngine;
using System;
using System.Collections.Generic;
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

        public int Cost => _cost;
        [SerializeField] int _cost;

        public PlayableAsset Timeline => _timeline;
        [SerializeField] PlayableAsset _timeline;

        public List<ActivationBindingData> ActivationBindingMap => _activationBindingMap;
        [SerializeField] List<ActivationBindingData> _activationBindingMap;
    }
}
