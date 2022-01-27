using UnityEngine;

namespace HexRPG.Battle
{
    public interface IVisibleController : IFeature
    {
        void SetVisible(bool visible);
    }

    public class VisibleController : AbstractCustomComponentBehaviour, IVisibleController
    {
        [Header("表示を操作したいオブジェクト。nullならこのオブジェクト。")]
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