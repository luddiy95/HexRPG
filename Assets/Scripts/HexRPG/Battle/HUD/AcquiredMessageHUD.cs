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

        IReadOnlyReactiveProperty<bool> CanAddNextMessage { get; }
        IObservable<Unit> OnComplete { get; }
    }

    public class AcquiredMessageHUD : AbstractPoolableMonoBehaviour<AcquiredMessageHUD>, IAcquiredMessage
    {
        [SerializeField] Transform _background;
        [SerializeField] Text _message;

        public IReadOnlyReactiveProperty<bool> CanAddNextMessage => _canAddNextMessage;
        readonly IReactiveProperty<bool> _canAddNextMessage = new ReactiveProperty<bool>(false);

        public IObservable<Unit> OnComplete => _onComplete;
        readonly ISubject<Unit> _onComplete = new Subject<Unit>();

        RectTransform _rectTransform;
        float _defaultPositionX;

        const float _messageAddInterval = 0.35f; // duration‚æ‚è’·‚­

        Tweener _hideTweener = null;
        const float _duration = 0.325f;

        const float _showInterval = 1.5f;

        void Awake()
        {
            _rectTransform = _background.GetComponent<RectTransform>();

            _defaultPositionX = _rectTransform.anchoredPosition.x;
            _rectTransform.anchoredPosition = Vector3.zero;
        }

        public void Show(string message)
        {
            _hideTweener = null;

            _canAddNextMessage.Value = false;
            _message.text = message;

            StartShowInterval(this.GetCancellationTokenOnDestroy()).Forget();

            _rectTransform.anchoredPosition = Vector3.zero;
            TransformUtility.DOAnchorPosX(_rectTransform, _defaultPositionX, _duration);
        }

        public void Hide()
        {
            if(_hideTweener == null)
            {
                _hideTweener = TransformUtility.DOAnchorPosX(_rectTransform, 0, _duration)
                    .OnComplete(() => _onComplete.OnNext(Unit.Default));
            }
        }

        async UniTaskVoid StartShowInterval(CancellationToken token)
        {
            StartMessageAddInterval(token).Forget();
            await UniTask.Delay((int)(_showInterval * 1000), cancellationToken: token);
            (this as IAcquiredMessage).Hide();
            return;
        }

        async UniTaskVoid StartMessageAddInterval(CancellationToken token)
        {
            await UniTask.Delay((int)(_messageAddInterval * 1000), cancellationToken: token);
            _canAddNextMessage.Value = true;
            return;
        }
    }
}
