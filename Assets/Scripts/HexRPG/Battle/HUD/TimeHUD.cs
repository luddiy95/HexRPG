using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using System;
using System.Linq;

namespace HexRPG.Battle.HUD
{
    public class TimeHUD : MonoBehaviour
    {
        IBattleObservable _battleObservable;
        IUpdateObservable _updateObservable;
        BattleData _battleData;

        float _curTime;
        IDisposable _disposable;

        [SerializeField] Text _minute;
        [SerializeField] Text _second;
        [SerializeField] Text _milliSecond;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable,
            IUpdateObservable updateObservable,
            BattleData battleData
        )
        {
            _battleObservable = battleObservable;
            _updateObservable = updateObservable;
            _battleData = battleData;
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
                    if (type == GameResultType.WIN)
                    {
                        var timeList = _battleData.timeList;
                        timeList.Add(_curTime);
                        _battleData.timeList = timeList.OrderBy(time => time).ToList();
                    }
                })
                .AddTo(this);
        }
    }
}
