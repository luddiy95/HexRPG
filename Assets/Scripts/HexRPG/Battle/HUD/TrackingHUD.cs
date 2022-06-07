using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.HUD
{
    public interface ITrackingHUD
    {
        Vector2 AnchoredPos { get; }
        Vector2 Offset { set; }
    };

    public class TrackingHUD : MonoBehaviour, ICharacterHUD, ITrackingHUD
    {
        ITransformController _characterTransform;
        IUpdateObservable _updateObservable;

        private Canvas _parentCanvas;
        private RectTransform _selfTransform;

        Vector2 ITrackingHUD.AnchoredPos => _selfTransform.anchoredPosition;

        Vector2 ITrackingHUD.Offset { set => _offset = value; }
        protected Vector2 _offset;

        [Inject]
        public void Construct(
        IUpdateObservable updateObservable
        )
        {
            _updateObservable = updateObservable;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            _parentCanvas = gameObject.NearestCanvas();
            _selfTransform = GetComponent<RectTransform>();

            _characterTransform = chara.TransformController;

            _updateObservable.OnUpdate((int)UPDATE_ORDER.CAMERA)
            .Subscribe(_ =>
            {
                var pos2d = UGuiUtility.WorldToCanvasLocal(Camera.main, _parentCanvas, _characterTransform.DisplayTransform.position);
                _selfTransform.anchoredPosition = pos2d + _offset;
            })
            .AddTo(this);
        }
    }
}
