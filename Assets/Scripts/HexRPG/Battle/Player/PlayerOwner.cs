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
    }

    public class PlayerOwner : MonoBehaviour, IPlayerComponentCollection
    {
        ICharacterInput IPlayerComponentCollection.CharacterInput => _characterInput;
        ICharacterInput _characterInput;
        IMemberController IPlayerComponentCollection.MemberController => _memberController;
        IMemberController _memberController;
        IMemberObservable IPlayerComponentCollection.MemberObservable => _memberObservable;
        IMemberObservable _memberObservable;
        ISelectSkillController IPlayerComponentCollection.SelectSkillController => _selectSkillController;
        ISelectSkillController _selectSkillController;
        ISelectSkillObservable IPlayerComponentCollection.SelectSkillObservable => _selectSkillObservable;
        ISelectSkillObservable _selectSkillObservable;
        ISkillObservable IPlayerComponentCollection.SkillObservable => _skillObservable;
        ISkillObservable _skillObservable;
        IPauseController IPlayerComponentCollection.PauseController => _pauseController;
        IPauseController _pauseController;
        IPauseObservable IPlayerComponentCollection.PauseObservable => _pauseObservable;
        IPauseObservable _pauseObservable;

        [Inject]
        public void Construct(
            ICharacterInput characterInput,
            IMemberController memberController,
            IMemberObservable memberObservable,
            ISelectSkillController selectSkillController,
            ISelectSkillObservable selectSkillObservable,
            ISkillObservable skillObservable,
            IPauseController pauseController,
            IPauseObservable pauseObservable
        )
        {
            _characterInput = characterInput;
            _memberController = memberController;
            _memberObservable = memberObservable;
            _selectSkillController = selectSkillController;
            _selectSkillObservable = selectSkillObservable;
            _skillObservable = skillObservable;
            _pauseController = pauseController;
            _pauseObservable = pauseObservable;
        }

        public class Factory : PlaceholderFactory<Transform, Vector3, PlayerOwner>
        {

        }
    }
}
