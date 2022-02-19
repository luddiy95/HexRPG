using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    public interface IMemberComponentCollection : ICharacterComponentCollection
    {
        IAnimatorController AnimatorController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }
        ISkillController SkillController { get; }
        IActiveController ActiveController { get; }
        IHealth Health { get; }
        IMental Mental { get; }
        IProfileSetting ProfileSetting { get; }
        IMoveSetting MoveSetting { get; }
    }

    public class MemberOwner : MonoBehaviour, IMemberComponentCollection
    {
        IAnimatorController IMemberComponentCollection.AnimatorController => _animatorController;
        IAnimatorController _animatorController;
        ISkillSpawnObservable IMemberComponentCollection.SkillSpawnObservable => _skillSpawnObservable;
        ISkillSpawnObservable _skillSpawnObservable;
        ISkillController IMemberComponentCollection.SkillController => _skillController;
        ISkillController _skillController;
        IActiveController IMemberComponentCollection.ActiveController => _activeController;
        IActiveController _activeController;
        IHealth IMemberComponentCollection.Health => _health;
        IHealth _health;
        IMental IMemberComponentCollection.Mental => _mental;
        IMental _mental;
        IProfileSetting IMemberComponentCollection.ProfileSetting => _profileSetting;
        IProfileSetting _profileSetting;
        IMoveSetting IMemberComponentCollection.MoveSetting => _moveSetting;
        IMoveSetting _moveSetting;

        [Inject]
        public void Construct(
            IAnimatorController animatorController,
            ISkillSpawnObservable skillSpawnObservable,
            ITransformController transformController,
            ISkillController skillController,
            IActiveController activeController,
            IHealth health,
            IMental mental,
            IProfileSetting profileSetting,
            IMoveSetting moveSetting
        )
        {
            _animatorController = animatorController;
            _skillSpawnObservable = skillSpawnObservable;
            _skillController = skillController;
            _activeController = activeController;
            _health = health;
            _mental = mental;
            _profileSetting = profileSetting;
            _moveSetting = moveSetting;
        }

        public class Factory : PlaceholderFactory<Transform, Vector3, MemberOwner>
        {

        }
    }
}
