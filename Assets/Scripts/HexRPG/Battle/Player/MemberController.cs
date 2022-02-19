using System.Collections.Generic;
using System.Linq;
using UniRx;
using System;
using Zenject;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HexRPG.Battle.Player
{
    using Member;
    using Skill;

    public interface IMemberObservable
    {
        IMemberComponentCollection[] MemberList { get; }
        IReadOnlyReactiveProperty<IMemberComponentCollection> CurMember { get; }

        IReadOnlyReactiveProperty<ISkillComponentCollection[]> CurMemberSkillList { get; }
    }

    public interface IMemberController
    {
        UniTask SpawnAllMember();
        void ChangeMember(int index);
    }

    public class MemberController : IMemberController, IMemberObservable, IInitializable, IDisposable
    {
        ITransformController _transformController;
        List<MemberOwner.Factory> _memberFactories;

        IMemberComponentCollection[] IMemberObservable.MemberList => _memberList;
        IMemberComponentCollection[] _memberList;

        IReadOnlyReactiveProperty<IMemberComponentCollection> IMemberObservable.CurMember => _curMember;
        readonly ReactiveProperty<IMemberComponentCollection> _curMember = new ReactiveProperty<IMemberComponentCollection>();

        IReadOnlyReactiveProperty<ISkillComponentCollection[]> IMemberObservable.CurMemberSkillList => _curMemberSkillList;
        readonly ReactiveProperty<ISkillComponentCollection[]> _curMemberSkillList = new ReactiveProperty<ISkillComponentCollection[]>();

        CompositeDisposable _disposables = new CompositeDisposable();

        public MemberController(
            ITransformController transformController,
            List<MemberOwner.Factory> memberFactories)
        {
            _transformController = transformController;
            _memberFactories = memberFactories;
        }

        void IInitializable.Initialize()
        {
            _curMember
                .Skip(1)
                .Subscribe(member =>
                {
                    _curMemberSkillList.Value = member.SkillController.SkillList;
                })
                .AddTo(_disposables);
        }

        async UniTask IMemberController.SpawnAllMember()
        {
            _memberList = _memberFactories.Select(factory => factory.Create(_transformController.SpawnRootTransform, Vector3.zero)).ToArray();
            // ‘S‚Ä‚ÌSkill‚ª¶¬‚³‚ê‚é‚Ì‚ð‘Ò‚Â
            await UniTask.WaitUntil(() => _memberList.All(member => member.SkillSpawnObservable.IsAllSkillSpawned));
        }

        void IMemberController.ChangeMember(int index)
        {
            for (int i = 0; i < _memberList.Length; i++)
            {
                _memberList[i].ActiveController.SetActive(i == index);
            }

            _curMember.Value = _memberList[index];
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}