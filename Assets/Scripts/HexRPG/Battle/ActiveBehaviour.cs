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
        [SerializeField] protected GameObject _gameObject;

        void Start()
        {
            if (_gameObject == null) _gameObject = gameObject;

            //! 最初は非表示
            SetActive(false);
        }

        void IActiveController.SetActive(bool visible)
        {
            SetActive(visible);
        }

        protected virtual void SetActive(bool visible)
        {
            _gameObject.SetActive(visible);
        }
    }
}