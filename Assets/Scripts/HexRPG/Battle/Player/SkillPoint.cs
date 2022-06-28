using UnityEngine;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    public interface ISkillPoint
    {
        int Max { get; }
        IReadOnlyReactiveProperty<int> Current { get; }

        IReadOnlyReactiveProperty<float> ChargeRate { get; }

        void Update(int cv);
    }

    public interface ISkillPointSetting
    {
        int Max { get; }
    }

    public class SkillPoint : ISkillPoint, IInitializable, IDisposable
    {
        ISkillPointSetting _setting;
        IUpdateObservable _updateObservable;

        int ISkillPoint.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> ISkillPoint.Current => _current;
        readonly ReactiveProperty<int> _current = new ReactiveProperty<int>();

        IReadOnlyReactiveProperty<float> ISkillPoint.ChargeRate => _chargeRate;
        readonly IReactiveProperty<float> _chargeRate = new ReactiveProperty<float>(0);

        float _chargeSpeed = 0.25f;

        CompositeDisposable _disposables = new CompositeDisposable();

        public SkillPoint(
            ISkillPointSetting setting,
            IUpdateObservable updateObservable
        )
        {
            _setting = setting;
            _updateObservable = updateObservable;
        }

        void IInitializable.Initialize()
        {
            _max = _setting.Max;
            _current.Value = _setting.Max;

            _updateObservable.OnUpdate((int)UPDATE_ORDER.SP_CHARGE)
                .Where(_ => _current.Value < _max)
                .Subscribe(_ =>
                {
                    var chargeRate = _chargeRate.Value;
                    chargeRate += _chargeSpeed * Time.deltaTime;

                    if (chargeRate >= 1)
                    {
                        chargeRate = 0;
                        (this as ISkillPoint).Update(1);
                    }

                    _chargeRate.Value = chargeRate;
                })
                .AddTo(_disposables);
        }

        void ISkillPoint.Update(int cv)
        {
            int value = 0;
            if (cv < 0) value = Mathf.Max(0, _current.Value + cv);
            else value = Mathf.Min(_max, _current.Value + cv);
            if (value >= _max) _chargeRate.Value = 0;
            _current.SetValueAndForceNotify(value);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
