using UnityEngine;
using UniRx;
using System;
using Zenject;

namespace HexRPG.Battle
{
    public interface IHealth
    {
        void Init();
        void ForceDie();

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
        readonly IReactiveProperty<int> _current = new ReactiveProperty<int>();

        public Health(IHealthSetting setting)
        {
            _setting = setting;
        }

        void IInitializable.Initialize()
        {
            (this as IHealth).Init();
        }

        void IHealth.Init()
        {
            _max = _setting.Max;
            _current.Value = _max;
        }

        void IHealth.ForceDie()
        {
            _current.Value = 0;
        }

        void IHealth.Update(int cv)
        {
            int value = _current.Value + cv;
            if(cv < 0) value = Mathf.Max(0, value);
            else value = Mathf.Min(_max, value);
            _current.Value = value;
        }
    }
}
