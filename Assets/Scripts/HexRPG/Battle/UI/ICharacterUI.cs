using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace HexRPG.Battle.UI
{
    public interface ICharacterUI
    {
        void Bind(ICharacterComponentCollection character);

        IObservable<Unit> OnBack { get; }
    }

    public static class ClickUtility
    {
        public static void OnClickListener(this GameObject gameObject, Action action, GameObject disposable)
        {
            ObservablePointerClickTrigger trigger;
            if (!gameObject.TryGetComponent(out trigger)) trigger = gameObject.AddComponent<ObservablePointerClickTrigger>();
            trigger
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    action();
                })
                .AddTo(disposable);
        }
    }
}