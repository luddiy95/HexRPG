using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Plugins.Options;
using DG.Tweening.Core;

namespace HexRPG.Battle.HUD
{
    public interface IAcquiredMessage
    {
        void Show(string message);
        void Hide();

        IObservable<Unit> OnDestroy { get; }
    }

    public class AcquiredMessageHUD : MonoBehaviour, IAcquiredMessage
    {
        [SerializeField] Transform _background;
        [SerializeField] Text _message;

        IObservable<Unit> IAcquiredMessage.OnDestroy => _onDestroy;
        readonly ISubject<Unit> _onDestroy = new Subject<Unit>();

        RectTransform _rectTransform;
        float _defaultPositionX;

        Tweener _hideTweener = null;
        const float _duration = 0.325f;
        const float _showInterval = 1.5f;

        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        void Awake()
        {
            _rectTransform = _background.GetComponent<RectTransform>();

            _defaultPositionX = _rectTransform.anchoredPosition.x;
            _rectTransform.anchoredPosition = Vector3.zero;
        }

        void IAcquiredMessage.Show(string message)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            StartShowInterval(_cancellationTokenSource.Token).Forget();

            _rectTransform.anchoredPosition = Vector3.zero;
            DOAnchorPosX(_rectTransform, _defaultPositionX, _duration);
            _message.text = message;
        }

        void IAcquiredMessage.Hide()
        {
            if(_hideTweener == null)
            {
                _hideTweener = DOAnchorPosX(_rectTransform, 0, _duration)
                    .OnComplete(() => DestroyImmediate(gameObject));
            }
        }

        async UniTaskVoid StartShowInterval(CancellationToken token)
        {
            await UniTask.Delay((int)(_showInterval * 1000), cancellationToken: token);
            (this as IAcquiredMessage).Hide();
        }

        void OnDestroy()
        {
            _onDestroy.OnNext(Unit.Default);
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPosX(RectTransform target, float endValue, float duration, bool snapping = false)
        {
            TweenerCore<Vector2, Vector2, VectorOptions> t = DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, new Vector2(endValue, 0), duration);
            t.SetOptions(AxisConstraint.X, snapping).SetTarget(target);
            return t;
        }
    }
}
