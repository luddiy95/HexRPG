using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    public interface ISkillPoint
    {
        int Max { get; }

        IReadOnlyReactiveProperty<int> Current { get; }

        void Update(int cv);
    }

    public interface ISkillPointSetting
    {
        int Max { get; }
    }

    public class SkillPoint : ISkillPoint, IInitializable
    {
        ISkillPointSetting _setting;

        int ISkillPoint.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> ISkillPoint.Current => _current;
        readonly ReactiveProperty<int> _current = new ReactiveProperty<int>();

        public SkillPoint(ISkillPointSetting setting)
        {
            _setting = setting;
        }

        void IInitializable.Initialize()
        {
            _max = _setting.Max;
            _current.Value = _setting.Max;
        }

        void ISkillPoint.Update(int cv)
        {
            int value = 0;
            if (cv < 0) value = Mathf.Max(0, _current.Value + cv);
            else value = Mathf.Min(_max, _current.Value + cv);
            _current.SetValueAndForceNotify(value);
        }
    }
}
