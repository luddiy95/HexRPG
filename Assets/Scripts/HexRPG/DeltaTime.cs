using UniRx;
using UnityEngine;

namespace HexRPG
{
    public interface IDeltaTime
    {
        float DeltaTime { get; }
        IReactiveProperty<float> TimeScale { get; }
    }

    public class DeltaTime : IDeltaTime
    {
        float IDeltaTime.DeltaTime => Time.deltaTime * _timeScale.Value;

        IReactiveProperty<float> IDeltaTime.TimeScale => _timeScale;

        private IReactiveProperty<float> _timeScale = new ReactiveProperty<float>(1f);
    }
}
