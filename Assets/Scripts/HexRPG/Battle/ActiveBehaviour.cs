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

        void Start()
        {
            if (_gameObject == null) _gameObject = gameObject;

            //! �ŏ��͔�\��
            (this as IActiveController).SetActive(false);
        }

        void IActiveController.SetActive(bool visible)
        {
            _gameObject.SetActive(visible);
        }
    }
}