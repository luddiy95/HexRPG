using UnityEngine;

namespace HexRPG.Battle
{
    public interface IActiveController
    {
        void SetActive(bool visible);
    }

    public class ActiveBehaviour : MonoBehaviour, IActiveController
    {
        GameObject GameObject => _gameObject ? _gameObject : gameObject;
        [Header("表示を操作したいオブジェクト。nullならこのオブジェクト。")]
        [SerializeField] protected GameObject _gameObject;

        void Start()
        {
            SetActive(false); // 最初は非表示
        }

        public void SetActive(bool visible)
        {
            GameObject.SetActive(visible);
        }
    }
}