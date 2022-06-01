using System.Collections.Generic;
using System.Linq;
using UniRx;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Member;

    public interface IMemberObservable
    {
        IReadOnlyReactiveCollection<IMemberComponentCollection> MemberList { get; }
        IReadOnlyReactiveProperty<IMemberComponentCollection> CurMember { get; }
        int CurMemberIndex { get; }
    }

    public interface IMemberController
    {
        UniTask SpawnAllMember(CancellationToken token);
        void ChangeMember(int index);
    }

    public class MemberController : IMemberController, IMemberObservable, IInitializable, IDisposable
    {
        ICharacterInput _characterInput;
        ITransformController _transformController;
        List<MemberOwner.Factory> _memberFactories;
        IAttackApplicator _attackApplicator;

        IReadOnlyReactiveCollection<IMemberComponentCollection> IMemberObservable.MemberList => _memberList;
        readonly IReactiveCollection<IMemberComponentCollection> _memberList = new ReactiveCollection<IMemberComponentCollection>();

        IReadOnlyReactiveProperty<IMemberComponentCollection> IMemberObservable.CurMember => _curMember;
        readonly IReactiveProperty<IMemberComponentCollection> _curMember = new ReactiveProperty<IMemberComponentCollection>();

        int IMemberObservable.CurMemberIndex => _curMemberIndex;
        int _curMemberIndex = 0;

        CompositeDisposable _disposables = new CompositeDisposable();

        public MemberController(
            ICharacterInput characterInput,
            ITransformController transformController,
            List<MemberOwner.Factory> memberFactories,
            IAttackApplicator attackApplicator
        )
        {
            _characterInput = characterInput;
            _transformController = transformController;
            _memberFactories = memberFactories;
            _attackApplicator = attackApplicator;
        }

        void IInitializable.Initialize()
        {
            _characterInput.SelectedMemberIndex
                .Subscribe(index => (this as IMemberController).ChangeMember(index))
                .AddTo(_disposables);
        }

        async UniTask IMemberController.SpawnAllMember(CancellationToken token)
        {
            _memberFactories.ForEach(factory =>
            {
                var member = factory.Create(_transformController.SpawnRootTransform("Member"), Vector3.zero).GetComponent<IMemberComponentCollection>();
                member.DieObservable.OnFinishDie
                    .Subscribe(_ => _memberList.Remove(member))
                    .AddTo(_disposables);
                _memberList.Add(member);
            });

            foreach (var member in _memberList) member.CombatSpawnController.Spawn(_attackApplicator);
            foreach (var member in _memberList) member.SkillSpawnController.Spawn(_transformController.SpawnRootTransform("Skill"));

            // 全てのCombat/Skillが生成されるのを待つ
            await UniTask.WaitUntil(
                () => _memberList.All(member => member.CombatSpawnObservable.isCombatSpawned && member.SkillSpawnObservable.IsAllSkillSpawned),
                cancellationToken: token);

            // 各MemberのAnimationBehaviour初期化(MemberのAnimator, Combat, Skillが必要)
            foreach (var member in _memberList) member.AnimationController.Init();
        }

        void IMemberController.ChangeMember(int index)
        {
            for (int i = 0; i < _memberList.Count; i++)
            {
                _memberList[i].ActiveController.SetActive(i == index);
            }

            _curMemberIndex = index;
            _curMember.Value = _memberList[index];
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}