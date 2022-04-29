using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class ObserverTest : MonoBehaviour
{
    [SerializeField] ObservableTest _observable;

    void Awake()
    {
        _observable.Value
            .Subscribe(value =>
            {
                Debug.Log(value);
            })
            .AddTo(this);
    }
}
