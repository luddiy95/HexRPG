using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace HexRPG.Battle
{
    using Player;

    public class BattleManager : AbstractCustomComponentBehaviour, IBattleObservable
    {
        [Header("�v���[���[�v���n�u")]
        [SerializeField] GameObject _playerPrefab;

        [Header("������CustomComponentCollection")]
        [SerializeField] GameObject[] _instances;

        IComponentCollectionFactory _factory = null;

        public IObservable<ICustomComponentCollection> OnPlayerSpawn => _onSpawnPlayer;

        readonly ISubject<ICustomComponentCollection> _onSpawnPlayer = new Subject<ICustomComponentCollection>();

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IBattleObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.QueryInterface(out _factory))
            {
                Main(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        async UniTaskVoid Main(CancellationToken token)
        {
            await UniTask.Yield(token);

            // ���u��GameObject��CustomComponentCollection�ɂ���
            foreach (var instance in _instances)
            {
                _factory.CreateComponentCollectionWithoutInstantiate(instance, null, null);
            }

            var player = Spawn(_playerPrefab, true);
            _onSpawnPlayer.OnNext(player);
        }

        ICustomComponentCollection Spawn(GameObject prefab, bool isPlayer)
        {
            var components = new List<ICustomComponent>
            {
                new ActionStateController(),
            };

            if (isPlayer)
            {
                components.AddRange(new List<ICustomComponent>
                {

                });
            }

            // �L�����N�^����
            var obj = _factory.CreateComponentCollection(prefab, components, owner =>
            {
                if (isPlayer && Owner.QueryInterface(out ICharacterInput input))
                {
                    owner.RegisterInterface(input);
                }
            });

            // �o���ʒu
            if(obj.QueryInterface(out ITransformController transformController))
            {
                transformController.Position = Vector3.zero;
                //TODO: Enemy�̏ꍇ
            }

            return obj;
        }
    }
}
