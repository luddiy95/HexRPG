using UnityEngine;
using System.Linq;

namespace HexRPG.Battle
{
    public interface IAudioController
    {
        void Play(string sound);
    }

    public class AudioManager : MonoBehaviour, IAudioController
    {
        [SerializeField]
        AudioMap _audioMap;
        AudioSource _audioSource;

        void Start()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        void IAudioController.Play(string audioName)
        {
            var audioClip = _audioMap.audioMap.FirstOrDefault(clip => clip.name == audioName);
            if (audioClip == null) return;
            _audioSource.clip = audioClip;
            _audioSource.Play();
        }
    }
}
