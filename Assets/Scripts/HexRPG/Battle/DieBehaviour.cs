using UnityEngine;
using UnityEngine.Playables;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Zenject;
using System;

namespace HexRPG.Battle
{
    public interface IDieController
    {
        void Init();
        void ForceDie();
    }

    public interface IDieObservable
    {
        IReadOnlyReactiveProperty<bool> IsDead { get; }
        IObservable<Unit> OnFinishDie { get; }
    }

    public class DieBehaviour : MonoBehaviour, IDieController, IDieObservable
    {
        IHealth _health;
        IDieSetting _dieSetting;
        IAnimationController _animationController;

        IReadOnlyReactiveProperty<bool> IDieObservable.IsDead => _isDead;
        readonly IReactiveProperty<bool> _isDead = new ReactiveProperty<bool>(false);

        IObservable<Unit> IDieObservable.OnFinishDie => _onFinishDie;
        readonly ISubject<Unit> _onFinishDie = new Subject<Unit>();

        [SerializeField] PlayableDirector _director;

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
            await UniTask.Yield(this.GetCancellationTokenOnDestroy()); // 他のコンポーネントが初期化されるのを待つ

            _director.playableAsset = _dieSetting.Timeline;
            _director.stopped += (obj) =>
            {
                FinishDie(this.GetCancellationTokenOnDestroy()).Forget();
            };

            _health.Current
                .Where(health => health <= 0)
                .Subscribe(_ => Die())
                .AddTo(this);
        }

        void IDieController.Init()
        {
            _isDead.Value = false;
        }

        void Die()
        {
            _isDead.Value = true;
            _director.Play();
            _animationController.Play("Die");
        }

        async UniTaskVoid FinishDie(CancellationToken token)
        {
            await UniTask.Delay(2000);

            _onFinishDie.OnNext(Unit.Default);

            return;
        }

        void IDieController.ForceDie()
        {
            if(_isDead.Value == false) Die();
        }
    }
}
