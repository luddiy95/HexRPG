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

        void Start()
        {
            if (_gameObject == null) _gameObject = gameObject;

            //! 最初は非表示
            (this as IActiveController).SetActive(false);
        }

        void IActiveController.SetActive(bool visible)
        {
            _gameObject.SetActive(visible);
        }
    }
}