using UnityEngine;
using UnityEditor;
using UniRx;
using Zenject;
using System.Linq;

namespace HexRPG.Battle.HUD
{
    public interface IFloatingHUD
    {
        Vector2 AnchoredPos { get; }
        Vector2 Offset { set; }
    };

    public class FloatingHUD : MonoBehaviour, ICharacterHUD, IFloatingHUD
    {
        ITransformController _characterTransform;
        IUpdateObservable _updateObservable;

        private Canvas _parentCanvas;
        private RectTransform _selfTransform;

        Vector2 IFloatingHUD.AnchoredPos => _selfTransform.anchoredPosition;

        Vector2 IFloatingHUD.Offset { set => _offset = value; }
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

            chara.DieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ => DestroyImmediate(gameObject))
                .AddTo(this);

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
