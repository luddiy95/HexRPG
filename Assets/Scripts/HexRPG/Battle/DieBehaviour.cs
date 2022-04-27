using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;

namespace HexRPG.Battle
{
    public interface IDieObservable
    {
        IReadOnlyReactiveProperty<bool> IsDead { get; }
    }

    public class DieBehaviour : MonoBehaviour, IDieObservable
    {
        IHealth _health;
        IDieSetting _dieSetting;
        IAnimationController _animationController;

        IReadOnlyReactiveProperty<bool> IDieObservable.IsDead => _isDead;
        readonly IReactiveProperty<bool> _isDead = new ReactiveProperty<bool>(false);

        [SerializeField] PlayableDirector _director;

        CancellationToken _cancellationToken;

        [Inject]
        public void Construct(
            IHealth health,
            IDieSetting dieSetting,
            IAnimationController animationController
        )
        {
            _health = health;
            _dieSetting = dieSetting;
            _animationController = animationController;
        }

        async UniTaskVoid Start()
        {
            _cancellationToken = this.GetCancellationTokenOnDestroy();
            await UniTask.Yield(_cancellationToken); // 他のコンポーネントが初期化されるのを待つ

            _director.playableAsset = _dieSetting.Timeline;
            _director.stopped += (obj) =>
            {
                DestroySelf(_cancellationToken).Forget();
            };

            _health.Current
                .Where(health => health <= 0)
                .Subscribe(_ =>
                {
                    _isDead.Value = true;
                    _director.Play();
                    _animationController.Play("Die");
                })
                .AddTo(this);
        }

        async UniTaskVoid DestroySelf(CancellationToken token)
        {
            await UniTask.Delay(2000);

            DestroyImmediate(gameObject);
        }
    }
}
