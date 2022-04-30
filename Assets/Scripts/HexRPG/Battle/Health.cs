using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle
{
    public interface IHealth
    {
        int Max { get; }

        IObservable<int> Current { get; }

        void Update(int cv);
    }

    public interface IHealthSetting
    {
        int Max { get; }
    }

    public class Health : IHealth, IInitializable, ITickable
    {
        IHealthSetting _setting;

        int IHealth.Max => _max;
        int _max;

        IObservable<int> IHealth.Current => _current;
        readonly ReactiveProperty<int> _current = new ReactiveProperty<int>();

        public Health(IHealthSetting setting)
        {
            _setting = setting;
        }

        void IInitializable.Initialize()
        {
            _max = _setting.Max;
            _current.Value = _setting.Max;
        }

        void IHealth.Update(int cv)
        {
            int value = 0;
            if(cv < 0) value = Mathf.Max(0, _current.Value + cv);
            else value = Mathf.Min(_max, _current.Value + cv);
            _current.SetValueAndForceNotify(value);
        }

        void ITickable.Tick()
        {
            if (Input.GetKeyDown(KeyCode.K)) _current.Value = 0;
        }
    }
}
