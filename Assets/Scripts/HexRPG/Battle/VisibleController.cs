using UnityEngine;

namespace HexRPG.Battle
{
    public interface IVisibleController : IFeature
    {
        void SetVisible(bool visible);
    }

    public class VisibleController : AbstractCustomComponentBehaviour, IVisibleController
    {
        [Header("�\���𑀍삵�����I�u�W�F�N�g�Bnull�Ȃ炱�̃I�u�W�F�N�g�B")]
        [SerializeField] GameObject _gameObject;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IVisibleController>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_gameObject == null) _gameObject = gameObject;
        }

        void IVisibleController.SetVisible(bool visible)
        {
            _gameObject.SetActive(visible);
        }
    }
}