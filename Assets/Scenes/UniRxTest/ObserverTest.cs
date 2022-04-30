using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class ObserverTest : MonoBehaviour
{
    [SerializeField] ObservableTest _observable;

    void Awake()
    {
        //Debug.Log(_observable.Value.Value);
        _observable.Value
            .Subscribe(value =>
            {
                Debug.Log(value.test);
            })
            .AddTo(this);

        /*
        _observable.Integer
            .Subscribe(value =>
            {
                Debug.Log(value);
            })
            .AddTo(this);
        */
    }
}
