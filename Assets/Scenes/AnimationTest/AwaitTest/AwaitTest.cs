using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class AwaitTest : MonoBehaviour
{
    CancellationTokenSource _cancellationTokenSource;

    async UniTaskVoid Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        await Method2(_cancellationTokenSource.Token);

        Debug.Log("Cancel3");
    }

    // ‘Ò‚¿‡‚í‚¹‚µ‚È‚¢
    async UniTask Method1(CancellationToken token)
    {
        await AsyncMethod1(token);

        Debug.Log("Canceled");
    }

    // ‘Ò‚¿‡‚í‚¹‚·‚é
    async UniTask Method2(CancellationToken token)
    {
        await AsyncMethod2(token);
        Debug.Log("cancel2");
    }

    async UniTask AsyncMethod1(CancellationToken token)
    {
        await UniTask.WaitWhile(() =>
        {
            Debug.Log("wait");
            return true;
        }, cancellationToken: token);

        Debug.Log("canceled");
    }

    async UniTask AsyncMethod2(CancellationToken token)
    {
        await UniTask.Delay(5000, cancellationToken: token);

        Debug.Log("cancel1");
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
