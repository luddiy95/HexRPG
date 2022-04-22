using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class AwaitTest2 : MonoBehaviour
{
    CancellationTokenSource _cancellationTokenSource;
    // Start is called before the first frame update
    void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        _cancellationTokenSource.Token.Register(() => Debug.Log("test1"));
        _cancellationTokenSource.Token.Register(() => Debug.Log("test2"));

        UniTask.WaitWhile(() => true, cancellationToken: _cancellationTokenSource.Token);
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
