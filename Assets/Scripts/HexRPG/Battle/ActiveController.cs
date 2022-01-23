using UnityEngine;

namespace HexRPG.Battle
{
    public interface IActiveController : IFeature
    {
        void SetActive(bool visible);
    }

    public class ActiveController : AbstractCustomComponentBehaviour, IActiveController
    {
        [Header("�\���𑀍삵�����I�u�W�F�N�g�Bnull�Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] GameObject _gameObject;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IActiveController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_gameObject == null) _gameObject = gameObject;
        }

        void IActiveController.SetActive(bool visible)
        {
            _gameObject.SetActive(visible);
        }
    }
}