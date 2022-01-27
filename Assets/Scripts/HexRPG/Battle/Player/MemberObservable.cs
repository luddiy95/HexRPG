using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Player
{
    public interface IMemberObservable : IFeature
    {
        ICustomComponentCollection[] MemberList { get; }
        IReadOnlyReactiveProperty<ICustomComponentCollection> CurMember { get; }

        ICustomComponentCollection[] CurMemberSkillList { get; }

        void ChangeMember(int index);
    }

    public class MemberObservable : AbstractCustomComponentBehaviour, IMemberObservable
    {
        ITransformController _transformController;

        ICustomComponentCollection[] IMemberObservable.MemberList => _memberList;
        ICustomComponentCollection[] _memberList;

        IReadOnlyReactiveProperty<ICustomComponentCollection> IMemberObservable.CurMember => _curMember;
        readonly ReactiveProperty<ICustomComponentCollection> _curMember = new ReactiveProperty<ICustomComponentCollection>();

        ICustomComponentCollection[] IMemberObservable.CurMemberSkillList => _curMemberSkillList;
        ICustomComponentCollection[] _curMemberSkillList;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMemberObservable>(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.QueryInterface(out _transformController);

            _curMember
                .Where(member => member != null)
                .Subscribe(member =>
                {
                    if (member.QueryInterface(out ISkillController skillController))
                    {
                        _curMemberSkillList = skillController.SkillList;
                    }
                })
                .AddTo(this);

            if (Owner.QueryInterface(out IPartySetting partySetting))
            {
                _memberList = partySetting.Party.Select(member => SpawnMember(member)).ToArray();
            }

            // CurMemberを設定するのはBattle開始してから(全てのCustomComponentCollectionを生成し終わってから)
            if(Owner.QueryInterface(out IBattleObservable battleObservable))
            {
                battleObservable.OnBattleStart
                    .First()
                    .Subscribe(_ => (this as IMemberObservable).ChangeMember(0))
                    .AddTo(this);
            }
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
                transformController.RootTransform.parent = _transformController.SpawnRootTransform;
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