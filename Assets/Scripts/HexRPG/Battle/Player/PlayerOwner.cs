using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player
{
    public interface IPlayerComponentCollection : ICharacterComponentCollection
    {
        ICharacterInput CharacterInput { get; }
        IMemberController MemberController { get; }
        IMemberObservable MemberObservable { get; }
        ISelectSkillController SelectSkillController { get; }
        ISelectSkillObservable SelectSkillObservable { get; }
        ISkillController SkillController { get; }
        ISkillObservable SkillObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class PlayerOwner : MonoBehaviour, IPlayerComponentCollection
    {
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        IHealth ICharacterComponentCollection.Health => (this as IPlayerComponentCollection).MemberObservable.CurMember.Value.Health;
        [Inject] ICharacterInput IPlayerComponentCollection.CharacterInput { get; }
        [Inject] IMemberController IPlayerComponentCollection.MemberController { get; }
        [Inject] IMemberObservable IPlayerComponentCollection.MemberObservable { get; }
        [Inject] ISelectSkillController IPlayerComponentCollection.SelectSkillController { get; }
        [Inject] ISelectSkillObservable IPlayerComponentCollection.SelectSkillObservable { get; }
        [Inject] ISkillController IPlayerComponentCollection.SkillController { get; }
        [Inject] ISkillObservable IPlayerComponentCollection.SkillObservable { get; }

        //TODO: Decorator—p
        [Inject] IActionStateObservable IPlayerComponentCollection.ActionStateObservable { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, PlayerOwner>
        {

        }
    }
}
