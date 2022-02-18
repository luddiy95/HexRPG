using UnityEngine;

namespace HexRPG.Battle
{
    public interface IActiveController
    {
        void SetActive(bool visible);
    }

    public class ActiveBehaviour : MonoBehaviour, IActiveController
    {
        [Header("�\���𑀍삵�����I�u�W�F�N�g�Bnull�Ȃ炱�̃I�u�W�F�N�g�B")]
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