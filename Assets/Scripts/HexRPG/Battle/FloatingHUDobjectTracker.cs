using UnityEngine;
using UniRx;

namespace HexRPG.Battle
{
    /// <summary>
    /// 頭上HUDとしてキャラクタに追従するコンポネント
    /// </summary>
    public class FloatingHUDobjectTracker : MonoBehaviour, ICharacterHUD
    {
        private Canvas _parentCanvas;
        private RectTransform _selfTransform;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            _parentCanvas = gameObject.NearestCanvas();
            _selfTransform = GetComponent<RectTransform>();

            //TODO:
            /*
            if (character.QueryInterface(out ITransformController characterTransform) == false)
            {
                return;
            }
            if (character.QueryInterface(out IUpdateObservable updateObservable) == false)
            {
                return;
            }

            updateObservable.OnUpdate((int)UPDATE_ORDER.CAMERA)
            .Subscribe(_ =>
            {
                var pos2d = UGuiUtility.WorldToCanvasLocal(Camera.main, _parentCanvas, characterTransform.Position);
                _selfTransform.anchoredPosition = pos2d;
            })
            .AddTo(this);
            */
        }
    }
}
