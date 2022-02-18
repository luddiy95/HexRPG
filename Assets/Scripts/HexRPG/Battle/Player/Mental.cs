using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    public interface IMental
    {
        int Max { get; }

        IReadOnlyReactiveProperty<int> Current { get; }

        void Update(int cv);
    }

    public interface IMentalSetting
    {
        int Max { get; }
    }

    public class Mental : IMental, IInitializable
    {
        IMentalSetting _setting;

        int IMental.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> IMental.Current => _current;
        ReactiveProperty<int> _current = new ReactiveProperty<int>();

        public Mental(IMentalSetting setting)
        {
            _setting = setting;
        }

        void IInitializable.Initialize()
        {
            _max = _setting.Max;
            _current.Value = _setting.Max;
        }

        void IMental.Update(int cv)
        {
            int value = 0;
            if (cv < 0) value = Mathf.Max(0, _current.Value + cv);
            else value = Mathf.Min(_max, _current.Value + cv);
            _current.SetValueAndForceNotify(value);
        }
    }
}
