using UnityEngine;

namespace HexRPG.Battle
{
    [CreateAssetMenu(fileName = "AudioMap", menuName = "ScriptableObjects/AudioMap")]
    public class AudioMap : ScriptableObject
    {
        public AudioClip[] audioMap;
    }
}
