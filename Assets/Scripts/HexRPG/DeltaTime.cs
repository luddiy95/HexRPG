using UniRx;
using UnityEngine;

namespace HexRPG
{
    public interface IDeltaTime : IFeature
    {
        float DeltaTime { get; }
        IReactiveProperty<float> TimeScale { get; }
    }

    public class DeltaTime : AbstractCustomComponent, IDeltaTime
    {
        float IDeltaTime.DeltaTime => Time.deltaTime * _timeScale.Value;

        IReactiveProperty<float> IDeltaTime.TimeScale => _timeScale;

        private IReactiveProperty<float> _timeScale = new ReactiveProperty<float>(1f);

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IDeltaTime>(this);
        }
    }
}
