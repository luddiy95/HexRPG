using UnityEngine.Events;
using UnityEngine;

namespace HexRPG
{
    public class ParticleSystemCallback : MonoBehaviour
    {
        [SerializeField] private UnityEvent stopCallback;

        private void OnParticleSystemStopped()
        {
            stopCallback.Invoke();
        }
    }
}
