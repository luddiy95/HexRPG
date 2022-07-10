using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Zenject;
using UniRx;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle.HUD
{
    public class ScorePanelHUD : MonoBehaviour
    {
        AcquiredMessageHUD.Factory _acquiredMessageFactory;
        IScoreObservable _scoreObservable;
        //TODO: Inspector用
        IScoreController _scoreController;

        [SerializeField] Text _scoreAmountText;
        [SerializeField] Text _scoreMaxText;

        [SerializeField] GameObject _scoreGaugeObj;
        IGauge _scoreGauge;

        readonly IReactiveCollection<IAcquiredMessage> _acquiredMessageList = new ReactiveCollection<IAcquiredMessage>();

        [Inject]
        public void Construct(
            AcquiredMessageHUD.Factory acquiredMessageFactory,
            IScoreObservable scoreObservable,
            IScoreController scoreController
        )
        {
            _acquiredMessageFactory = acquiredMessageFactory;
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
                    string message = scoreData.scoreInfo.message + " +" + scoreData.scoreInfo.score * scoreData.count;
                    TryAddMessage(message);
                })
                .AddTo(this);
        }

        void TryAddMessage(string message)
        {
            if (_acquiredMessageList.Count > 0)
            {
                var lastMessage = _acquiredMessageList.Last();
                WaitForAddMessage(lastMessage, message);
            }
            else
            {
                AddMessage(message);
            }
        }

        void WaitForAddMessage(IAcquiredMessage lastMessage, string message)
        {
            lastMessage.CanAddNextMessage
                .Where(canAdd => canAdd)
                .First()
                .Subscribe(_ =>
                {
                    if (_acquiredMessageList.Count > 0 && lastMessage != _acquiredMessageList.Last())
                    {
                        TryAddMessage(message);
                        return;
                    }

                    try
                    {
                        AddMessage(message);
                    }
                    catch
                    {
                        // 最大数を超えた場合は一番上のメッセージをhideして、OnCompleteしたら新たに表示
                        var headMessage = _acquiredMessageList[0];
                        headMessage.OnComplete
                            .First()
                            .Subscribe(_ => TryAddMessage(message))
                            .AddTo(this);
                        headMessage.CanAddNextMessage
                            .Where(canAdd => canAdd)
                            .First()
                            .Subscribe(_ =>
                            {
                                headMessage.Hide();
                            })
                            .AddTo(this);
                    }
                })
                .AddTo(this);
        }

        void AddMessage(string message)
        {
            var hud = _acquiredMessageFactory.Create();
            hud.transform.SetAsLastSibling();
            hud.OnComplete
                .First()
                .Subscribe(_ =>
                {
                    _acquiredMessageList.Remove(hud);
                    hud.Dispose();
                })
                .AddTo(this);
            _acquiredMessageList.Add(hud);
            hud.Show(message);
        }

#if UNITY_EDITOR

        ScoreType type = ScoreType.LIBERATE;
        int count = 0;

        int index = 0;

        public void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                type = (ScoreType)EditorGUILayout.EnumPopup("ScoreType", type);
                GUILayout.Label("Count");
                count = EditorGUILayout.IntField(count);
                if (GUILayout.Button("OnNext"))
                {
                    AcquireScores(this.GetCancellationTokenOnDestroy()).Forget();
                }
            }
        }

        async UniTaskVoid AcquireScores(CancellationToken token)
        {
            _scoreController.AcquireScore(type, 0);
            await UniTask.Delay(400, cancellationToken: token);
            _scoreController.AcquireScore(type, 1);
            await UniTask.Delay(300, cancellationToken: token);
            _scoreController.AcquireScore(type, 2);
            //await UniTask.Delay(100, cancellationToken: token);
            _scoreController.AcquireScore(type, 3);
            //await UniTask.Delay(100, cancellationToken: token);
            _scoreController.AcquireScore(type, 4);
            //await UniTask.Delay(100, cancellationToken: token);
            _scoreController.AcquireScore(type, 5);
            await UniTask.Delay(1000, cancellationToken: token);
            _scoreController.AcquireScore(type, 6);
            //await UniTask.Delay(100, cancellationToken: token);
            _scoreController.AcquireScore(type, 7);
        }

        [CustomEditor(typeof(ScorePanelHUD))]
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

                ((ScorePanelHUD)target).OnInspectorGUI();
            }
        }

#endif
    }
}
