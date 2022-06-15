using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

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

        CancellationToken _token;

        void Awake()
        {
            _rectTransform = _background.GetComponent<RectTransform>();

            _defaultPositionX = _rectTransform.anchoredPosition.x;
            _rectTransform.anchoredPosition = Vector3.zero;

            _token = this.GetCancellationTokenOnDestroy();
        }

        void IAcquiredMessage.Show(string message)
        {
            StartShowInterval(_token).Forget();

            _rectTransform.anchoredPosition = Vector3.zero;
            TransformUtility.DOAnchorPosX(_rectTransform, _defaultPositionX, _duration);
            _message.text = message;
        }

        void IAcquiredMessage.Hide()
        {
            if(_hideTweener == null)
            {
                _hideTweener = TransformUtility.DOAnchorPosX(_rectTransform, 0, _duration)
                    .OnComplete(() => Destroy(gameObject));
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
        }
    }
}
