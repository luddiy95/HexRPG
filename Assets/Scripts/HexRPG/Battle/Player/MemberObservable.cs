using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Skill;

    public interface IMemberObservable : IFeature
    {
        ICustomComponentCollection[] MemberList { get; }
        IReadOnlyReactiveProperty<ICustomComponentCollection> CurMember { get; }

        BaseSkill[] CurMemberSkillList { get; }

        void ChangeMember(int index);
    }

    public class MemberObservable : AbstractCustomComponentBehaviour, IMemberObservable
    {
        [SerializeField] Transform _memberRoot;

        ICustomComponentCollection[] IMemberObservable.MemberList => _memberList;
        ICustomComponentCollection[] _memberList;

        IReadOnlyReactiveProperty<ICustomComponentCollection> IMemberObservable.CurMember => _curMember;
        readonly ReactiveProperty<ICustomComponentCollection> _curMember = new ReactiveProperty<ICustomComponentCollection>();

        BaseSkill[] IMemberObservable.CurMemberSkillList => _curMemberSkillList;
        BaseSkill[] _curMemberSkillList;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMemberObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            _curMember
                .Where(member => member != null)
                .Subscribe(member =>
                {
                    if (member.QueryInterface(out ISkillListSetting skillListSetting))
                    {
                        _curMemberSkillList = skillListSetting.SkillList;
                    }
                })
                .AddTo(this);

            if (Owner.QueryInterface(out IPartySetting partySetting))
            {
                _memberList = partySetting.Party.Select(member => SpawnMember(member)).ToArray();
            }

            (this as IMemberObservable).ChangeMember(0);
        }

        ICustomComponentCollection SpawnMember(GameObject prefab)
        {
            if (!Owner.QueryInterface(out IComponentCollectionFactory factory)) return null;

            var components = new List<ICustomComponent>
            {
                new Health(),
                new Mental()
            };

            // キャラクタ生成
            var obj = factory.CreateComponentCollection(prefab, components, owner =>
            {

            });

            // 出現位置
            if (obj.QueryInterface(out ITransformController transformController))
            {
                transformController.Transform.parent = _memberRoot;
            }

            return obj;
        }

        void IMemberObservable.ChangeMember(int index)
        {
            for (int i = 0; i < _memberList.Length; i++)
            {
                if (_memberList[i].QueryInterface(out IActiveController activeController))
                {
                    activeController.SetActive(i == index);
                }
            }

            _curMember.Value = _memberList[index];
        }
    }
}