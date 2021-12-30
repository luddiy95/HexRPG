using UnityEngine.Events;
using UnityEngine;

public class ParticleSystemCallback : MonoBehaviour
{
    [SerializeField] private UnityEvent stopCallback;

    private void OnParticleSystemStopped()
    {
        stopCallback.Invoke();
    }
}
