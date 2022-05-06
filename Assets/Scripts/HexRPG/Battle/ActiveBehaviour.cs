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
        [SerializeField] protected GameObject _gameObject;

        void Start()
        {
            if (_gameObject == null) _gameObject = gameObject;

            //! �ŏ��͔�\��
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