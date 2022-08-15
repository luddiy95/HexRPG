using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using System;

namespace HexRPG.Battle.HUD
{
    public class TimeHUD : MonoBehaviour
    {
        BattleData _battleData;
        IBattleObservable _battleObservable;
        IUpdateObservable _updateObservable;

        float _curTime;
        IDisposable _disposable;

        [SerializeField] Text _minute;
        [SerializeField] Text _second;
        [SerializeField] Text _milliSecond;

        [Inject]
        public void Construct(
            BattleData battleData,
            IBattleObservable battleObservable,
            IUpdateObservable updateObservable
        )
        {
            _battleData = battleData;
            _battleObservable = battleObservable;
            _updateObservable = updateObservable;
        }

        void Start()
        {
            _battleObservable.OnBattleStart
                .Subscribe(_ =>
                {
                    var startTime = Time.timeSinceLevelLoad;

                    _disposable = _updateObservable.OnUpdate((int)UPDATE_ORDER.TIME)
                        .Subscribe(_ =>
                        {
                            _curTime = Time.timeSinceLevelLoad - startTime;
                            _minute.text = _curTime.GetMinute();
                            _second.text = _curTime.GetSecond();
                            _milliSecond.text = _curTime.GetMilliSecond();
                        });
                })
                .AddTo(this);

            _battleObservable.GameResultType
                .Subscribe(type =>
                {
                    _disposable?.Dispose();
                    if (type == GameResultType.CLEAR)
                    {
                        _battleData.battleClearDataMap.Add(new BattleData.BattleClearData
                        {
                            battleName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                            time = _curTime
                        });
                    }
                })
                .AddTo(this);
        }
    }
}
