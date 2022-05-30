using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player
{
    public interface IPlayerComponentCollection : ICharacterComponentCollection
    {
        IMemberController MemberController { get; }
        IMemberObservable MemberObservable { get; }
        IAttackObservable AttackObservable { get; }
        ILiberateObservable LiberateObservable { get; }
        ICharacterActionStateController CharacterActionStateController { get; }
        ISelectSkillObservable SelectSkillObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class PlayerOwner : MonoBehaviour, IPlayerComponentCollection
    {
        IProfileSetting ICharacterComponentCollection.ProfileSetting => MemberOwner.ProfileSetting;
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        IHealth ICharacterComponentCollection.Health => MemberOwner.Health;
        [Inject] IDamageApplicable ICharacterComponentCollection.DamagedApplicable { get; }
        [Inject] IMemberController IPlayerComponentCollection.MemberController { get; }
        [Inject] IMemberObservable IPlayerComponentCollection.MemberObservable { get; }
        [Inject] IAttackObservable IPlayerComponentCollection.AttackObservable { get; }
        [Inject] ILiberateObservable IPlayerComponentCollection.LiberateObservable { get; }
        [Inject] ICharacterActionStateController IPlayerComponentCollection.CharacterActionStateController { get; }
        [Inject] ISelectSkillObservable IPlayerComponentCollection.SelectSkillObservable { get; }

        //TODO: Decorator—p
        [Inject] IActionStateObservable IPlayerComponentCollection.ActionStateObservable { get; }

        ICharacterComponentCollection MemberOwner => (this as IPlayerComponentCollection).MemberObservable.CurMember.Value;

        public class Factory : PlaceholderFactory<Transform, Vector3, PlayerOwner>
        {

        }
    }
}
