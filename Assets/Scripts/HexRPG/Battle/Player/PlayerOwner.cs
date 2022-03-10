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
        ISkillObservable SkillObservable { get; }
        IPauseController PauseController { get; }
        IPauseObservable PauseObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class PlayerOwner : MonoBehaviour, IPlayerComponentCollection
    {
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] ICharacterInput IPlayerComponentCollection.CharacterInput { get; }
        [Inject] IMemberController IPlayerComponentCollection.MemberController { get; }
        [Inject] IMemberObservable IPlayerComponentCollection.MemberObservable { get; }
        [Inject] ISelectSkillController IPlayerComponentCollection.SelectSkillController { get; }
        [Inject] ISelectSkillObservable IPlayerComponentCollection.SelectSkillObservable { get; }
        [Inject] ISkillObservable IPlayerComponentCollection.SkillObservable { get; }
        [Inject] IPauseController IPlayerComponentCollection.PauseController { get; }
        [Inject] IPauseObservable IPlayerComponentCollection.PauseObservable { get; }

        //TODO: Decorator—p
        [Inject] IActionStateObservable IPlayerComponentCollection.ActionStateObservable { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, PlayerOwner>
        {

        }
    }
}
