using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace HexRPG.Battle
{
    public interface ISkillsEquipment
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

        public ActivationBindingObjDictionary ActivationBindingObjMap => _activationBindingObjMap;
        [SerializeField] ActivationBindingObjDictionary _activationBindingObjMap;
    }
}
