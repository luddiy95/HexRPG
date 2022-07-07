using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class DisposableTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CompositeDisposable disposables = new CompositeDisposable();
        disposables.Dispose();
        disposables.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
