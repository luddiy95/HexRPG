using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.HUD
{
    /// <summary>
    /// 頭上HUDとしてキャラクタに追従するコンポネント
    /// </summary>
    public class FloatingHUDobjectTracker : MonoBehaviour, ICharacterHUD
    {
        private Canvas _parentCanvas;
        private RectTransform _selfTransform;

        ITransformController _characterTransform;
        IUpdateObservable _updateObservable;

        [Inject]
        public void Construct(IUpdateObservable updateObservable)
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
                var pos2d = UGuiUtility.WorldToCanvasLocal(Camera.main, _parentCanvas, _characterTransform.Position);
                _selfTransform.anchoredPosition = pos2d;
            })
            .AddTo(this);

            chara.DieObservable.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ => DestroyImmediate(gameObject))
                .AddTo(this);
        }
    }
}
