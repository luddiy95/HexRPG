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
        //TODO: Inspectoróp
        IScoreController _scoreController;

        [SerializeField] Text _scoreAmountText;
        [SerializeField] Text _scoreMaxText;

        [SerializeField] GameObject _scoreGaugeObj;
        IGauge _scoreGauge;

        [SerializeField] Transform _acquiredMessageList;
        [SerializeField] GameObject _acquiredMessagePrefab;
        IAcquiredMessage CurAcquiredMessageHead => _acquiredMessageList.GetChild(0).GetComponent<IAcquiredMessage>();

        const int _maxShowMessageCount = 5;

        const float _messageAddInterval = 0.35f; //! AcquiredMessageHUD#durationÇÊÇËí∑Ç≠
        float _lastMessageAddedTime;

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
            var scoreMax = _scoreObservable.ScoreMax;
            _scoreMaxText.text = scoreMax.ToString();
            _scoreGauge = _scoreGaugeObj.GetComponent<IGauge>();
            _scoreGauge.Init(scoreMax);
            _scoreObservable.CurScore
                .Subscribe(score =>
                {
                    _scoreAmountText.text = score.ToString();
                    _scoreGauge.Set(score);
                })
                .AddTo(this);

            _scoreObservable.OnAddScoreData
                .Subscribe(scoreData =>
                {
                    OnAddScoreData(scoreData, this.GetCancellationTokenOnDestroy()).Forget();
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
                CurAcquiredMessageHead.Hide(); // Å¶Ç≥Ç∑Ç™Ç…HeadÇ™ï\é¶ìríÜÇ∆Ç¢Ç§Ç±Ç∆ÇÕÇ»Ç¢
            }
            else
            {
                AddMessageHUD();
            }
        }

        void OnDestroy()
        {
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
                count = EditorGUILayout.IntField(count);
                if (GUILayout.Button("OnNext"))
                {
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
                    _scoreController.AcquireScore(type, count);
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
