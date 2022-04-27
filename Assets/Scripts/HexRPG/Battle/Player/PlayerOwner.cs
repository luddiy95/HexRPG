using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player
{
    public interface IPlayerComponentCollection : ICharacterComponentCollection
    {
        IMemberController MemberController { get; }
        IMemberObservable MemberObservable { get; }
        ISelectSkillObservable SelectSkillObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class PlayerOwner : MonoBehaviour, IPlayerComponentCollection
    {
        IDieObservable ICharacterComponentCollection.DieObservable => null;
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        IHealth ICharacterComponentCollection.Health => MemberOwner.Health;
        [Inject] IMemberController IPlayerComponentCollection.MemberController { get; }
        [Inject] IMemberObservable IPlayerComponentCollection.MemberObservable { get; }
        [Inject] ISelectSkillObservable IPlayerComponentCollection.SelectSkillObservable { get; }

        //TODO: Decorator—p
        [Inject] IActionStateObservable IPlayerComponentCollection.ActionStateObservable { get; }

        ICharacterComponentCollection MemberOwner => (this as IPlayerComponentCollection).MemberObservable.CurMember.Value;

        public class Factory : PlaceholderFactory<Transform, Vector3, PlayerOwner>
        {

        }
    }
}
