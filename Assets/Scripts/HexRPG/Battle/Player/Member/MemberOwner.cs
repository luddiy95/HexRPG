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
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IAnimatorController IMemberComponentCollection.AnimatorController { get; }
        [Inject] ISkillSpawnObservable IMemberComponentCollection.SkillSpawnObservable { get; }
        [Inject] ISkillController IMemberComponentCollection.SkillController { get; }
        [Inject] IActiveController IMemberComponentCollection.ActiveController { get; }
        [Inject] IHealth IMemberComponentCollection.Health { get; }
        [Inject] IMental IMemberComponentCollection.Mental { get; }
        [Inject] IProfileSetting IMemberComponentCollection.ProfileSetting { get; }
        [Inject] IMoveSetting IMemberComponentCollection.MoveSetting { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, MemberOwner>
        {

        }
    }
}
