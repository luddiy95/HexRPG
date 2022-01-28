using UnityEngine;
using UniRx;

namespace HexRPG.Battle
{
    public interface IHealth : IFeature
    {
        int Max { get; }

        IReadOnlyReactiveProperty<int> Current { get; }

        void Update(int cv);
    }

    public interface IHealthSetting : IFeature
    {
        int Max { get; }
    }

    public class Health : AbstractCustomComponent, IHealth
    {
        int IHealth.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> IHealth.Current => _current;
        ReactiveProperty<int> _current = new ReactiveProperty<int>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);
            owner.RegisterInterface<IHealth>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.QueryInterface(out IHealthSetting setting) == true)
            {
                _max = setting.Max;
                _current.Value = setting.Max;
            }
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
