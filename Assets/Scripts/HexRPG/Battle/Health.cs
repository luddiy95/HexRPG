using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public interface IHealth
    {
        int Max { get; }

        IReadOnlyReactiveProperty<int> Current { get; }

        void Update(int cv);
    }

    public interface IHealthSetting
    {
        int Max { get; }
    }

    public class Health : IHealth, IInitializable
    {
        IHealthSetting _setting;

        int IHealth.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> IHealth.Current => _current;
        ReactiveProperty<int> _current = new ReactiveProperty<int>();

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
    }
}
