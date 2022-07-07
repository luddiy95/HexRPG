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
        [Header("�\���𑀍삵�����I�u�W�F�N�g�Bnull�Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] protected GameObject _gameObject;

        void Start()
        {
            //! �ŏ��͔�\��
            SetActive(false);
        }

        void IActiveController.SetActive(bool visible)
        {
            SetActive(visible);
        }

        protected virtual void SetActive(bool visible)
        {
            GameObject.SetActive(visible);
        }
    }
}