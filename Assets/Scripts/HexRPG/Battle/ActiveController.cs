using UnityEngine;

namespace HexRPG.Battle
{
    public interface IActiveController : IFeature
    {
        void SetActive(bool visible);
    }

    public class ActiveController : AbstractCustomComponentBehaviour, IActiveController
    {
        [Header("表示を操作したいオブジェクト。nullならこのオブジェクト。")]
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