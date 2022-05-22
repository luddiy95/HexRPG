using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Zenject;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.HUD
{
    public class ScoreHUD : MonoBehaviour
    {
        IScoreObservable _scoreObservable;
        //TODO: Inspector用
        IScoreController _scoreController;

        [SerializeField] Text _scoreAmountText;
        [SerializeField] Text _scoreMaxText;
        [SerializeField] GameObject _scoreGauge;
        [SerializeField] Transform _acquiredMessageList;

        [SerializeField] GameObject _acquiredMessagePrefab;
        IAcquiredMessage CurAcquiredMessageHead => _acquiredMessageList.GetChild(0).GetComponent<IAcquiredMessage>();

        const int _maxShowMessageCount = 5;

        const float _messageAddInterval = 0.35f; //! AcquiredMessageHUD#durationより長く
        float _lastMessageAddedTime;

        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            IScoreObservable scoreObservable,
            IScoreController scoreController
        )
        {
            _scoreObservable = scoreObservable;
            _scoreController = scoreController;
        }

        void Start()
        {
            _scoreObservable.OnAddScoreData
                .Subscribe(scoreData =>
                {
                    OnAddScoreData(scoreData, _cancellationTokenSource.Token).Forget();
                })
                .AddTo(this);
        }

        async UniTaskVoid OnAddScoreData(ScoreData scoreData, CancellationToken token)
        {
            if (Time.timeSinceLevelLoad - _lastMessageAddedTime < _messageAddInterval)
            {
                await UniTask.WaitWhile(() => Time.timeSinceLevelLoad - _lastMessageAddedTime < _messageAddInterval, cancellationToken: token);
            }
            AddMessage(scoreData);
        }

        void AddMessage(ScoreData scoreData)
        {
            void AddMessageHUD()
            {
                string message = scoreData.scoreInfo.message + " +" + scoreData.scoreInfo.score * scoreData.count;

                var hud = Instantiate(_acquiredMessagePrefab, _acquiredMessageList).GetComponent<IAcquiredMessage>();
                hud.Show(message);
            }

            _lastMessageAddedTime = Time.timeSinceLevelLoad;

            if (_acquiredMessageList.childCount == _maxShowMessageCount)
            {
                _disposables.Clear();
                CurAcquiredMessageHead.OnDestroy
                    .First()
                    .Subscribe(_ =>
                    {
                        AddMessageHUD();
                        _disposables.Clear();
                    })
                    .AddTo(_disposables);
                CurAcquiredMessageHead.Hide(); // ※さすがにHeadが表示途中ということはない
            }
            else
            {
                AddMessageHUD();
            }
        }

        void OnDestroy()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _disposables.Dispose();
        }

#if UNITY_EDITOR

        ScoreType type = ScoreType.LIBERATE;
        int count = 0;

        public void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                type = (ScoreType)EditorGUILayout.EnumPopup("ScoreType", type);
                GUILayout.Label("Count");
                EditorGUILayout.IntField(count);
                if (GUILayout.Button("OnNext"))
                {
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                    _scoreController.AquireScore(type, count);
                }
            }
        }

        [CustomEditor(typeof(ScoreHUD))]
        public class ScoreHUDInspector : Editor
        {
            private void OnEnable()
            {
            }

            private void OnDisable()
            {
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((ScoreHUD)target).OnInspectorGUI();
            }
        }

#endif
    }
}
