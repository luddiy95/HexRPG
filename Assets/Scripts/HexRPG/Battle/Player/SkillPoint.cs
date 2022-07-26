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

        void Update(int cv);
    }

    public interface ISkillPointSetting
    {
        int Max { get; }
    }

    public class SkillPoint : ISkillPoint, IInitializable, IDisposable
    {
        ISkillPointSetting _setting;
        IAttackObservable _attackObservable;
        IDamageApplicable _damagedApplicable;

        int ISkillPoint.Max => _max;
        int _max;

        IReadOnlyReactiveProperty<int> ISkillPoint.Current => _current;
        readonly ReactiveProperty<int> _current = new ReactiveProperty<int>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public SkillPoint(
            ISkillPointSetting setting,
            IAttackObservable attackObservable,
            IDamageApplicable damageApplicable
        )
        {
            _setting = setting;
            _attackObservable = attackObservable;
            _damagedApplicable = damageApplicable;
        }

        void IInitializable.Initialize()
        {
            _max = _setting.Max;
            _current.Value = _max;

            Observable.Merge(_attackObservable.OnAttackHit, _damagedApplicable.OnHit)
                .Subscribe(hitData =>
                {
                    int getAmount = hitData.HitType switch
                    {
                        HitType.NORMAL => 3,
                        HitType.WEAK => 5,
                        HitType.RESIST => 2,
                        HitType.CRITICAL => 5,
                        _ => 0
                    };
                    Update(getAmount);
                })
                .AddTo(_disposables);
        }

        public void Update(int cv)
        {
            int value = 0;
            if (cv < 0) value = Mathf.Max(0, _current.Value + cv);
            else value = Mathf.Min(_max, _current.Value + cv);
            _current.SetValueAndForceNotify(value);
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
