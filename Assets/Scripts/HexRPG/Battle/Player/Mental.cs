using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Player
{
    public interface IMental : IFeature
    {
        int Max { get; }

        IReadOnlyReactiveProperty<int> Current { get; }

        void Update(int cv);
    }

    public interface IMentalSetting : IFeature
    {
        int Max { get; }
    }

    public class Mental : AbstractCustomComponent, IMental
    {
        int IMental.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> IMental.Current => _current;
        ReactiveProperty<int> _current = new ReactiveProperty<int>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMental>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.QueryInterface(out IMentalSetting setting) == true)
            {
                _max = setting.Max;
                _current.Value = setting.Max;
            }
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
