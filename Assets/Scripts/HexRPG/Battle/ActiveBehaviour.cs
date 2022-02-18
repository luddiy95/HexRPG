using UnityEngine;

namespace HexRPG.Battle
{
    public interface IActiveController
    {
        void SetActive(bool visible);
    }

    public class ActiveBehaviour : MonoBehaviour, IActiveController
    {
        [Header("表示を操作したいオブジェクト。nullならこのオブジェクト。")]
        [SerializeField] GameObject _gameObject;

        void Awake()
        {
            if (_gameObject == null) _gameObject = gameObject;
        }

        void IActiveController.SetActive(bool visible)
        {
            _gameObject.SetActive(visible);
        }
    }
}