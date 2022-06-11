using System;
using UnityEngine;
using Kogane;

namespace HexRPG.Battle
{
    [Serializable] public class ActivationBindingObjValuePair : SerializableKeyValuePair<string, GameObject> { }
    [Serializable] public class ActivationBindingObjDictionary : SerializableDictionary<string, GameObject, ActivationBindingObjValuePair> { }
}
