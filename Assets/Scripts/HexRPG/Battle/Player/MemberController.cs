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
        int CurMemberIndex { get; }
    }

    public interface IMemberController
    {
        UniTask SpawnAllMember();
        void ChangeMember(int index);
    }

    public class MemberController : IMemberController, IMemberObservable, IDisposable
    {
        ITransformController _transformController;
        List<MemberOwner.Factory> _memberFactories;

        IMemberComponentCollection[] IMemberObservable.MemberList => _memberList;
        IMemberComponentCollection[] _memberList;

        IReadOnlyReactiveProperty<IMemberComponentCollection> IMemberObservable.CurMember => _curMember;
        readonly IReactiveProperty<IMemberComponentCollection> _curMember = new ReactiveProperty<IMemberComponentCollection>();

        int IMemberObservable.CurMemberIndex => _curMemberIndex;
        int _curMemberIndex = 0;

        CompositeDisposable _disposables = new CompositeDisposable();

        public MemberController(
            ITransformController transformController,
            List<MemberOwner.Factory> memberFactories)
        {
            _transformController = transformController;
            _memberFactories = memberFactories;
        }

        async UniTask IMemberController.SpawnAllMember()
        {
            _memberList = _memberFactories.Select(factory => factory.Create(_transformController.SpawnRootTransform("Member"), Vector3.zero)).ToArray();
            // ‘S‚Ä‚ÌSkill‚ª¶¬‚³‚ê‚é‚Ì‚ð‘Ò‚Â
            await UniTask.WaitUntil(() => _memberList.All(member => member.SkillSpawnObservable.IsAllSkillSpawned));
        }

        void IMemberController.ChangeMember(int index)
        {
            for (int i = 0; i < _memberList.Length; i++)
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