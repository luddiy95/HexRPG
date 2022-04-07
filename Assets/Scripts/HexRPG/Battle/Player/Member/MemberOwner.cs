using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    public interface IMemberComponentCollection : ICharacterComponentCollection
    {
        IAnimatorController AnimatorController { get; }
        IAnimationController AnimationController { get; }
        ICombatSpawnObservable CombatSpawnObservable { get; }
        ICombatController CombatController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }
        ISkillController SkillController { get; }
        IActiveController ActiveController { get; }
        IMental Mental { get; }
        IProfileSetting ProfileSetting { get; }
        IMoveSetting MoveSetting { get; }
    }

    public class MemberOwner : MonoBehaviour, IMemberComponentCollection
    {
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IHealth ICharacterComponentCollection.Health { get; }
        [Inject] IAnimatorController IMemberComponentCollection.AnimatorController { get; }
        [Inject] IAnimationController IMemberComponentCollection.AnimationController { get; }
        [Inject] ICombatSpawnObservable IMemberComponentCollection.CombatSpawnObservable { get; }
        [Inject] ICombatController IMemberComponentCollection.CombatController { get; }
        [Inject] ISkillSpawnObservable IMemberComponentCollection.SkillSpawnObservable { get; }
        [Inject] ISkillController IMemberComponentCollection.SkillController { get; }
        [Inject] IActiveController IMemberComponentCollection.ActiveController { get; }
        [Inject] IMental IMemberComponentCollection.Mental { get; }
        [Inject] IProfileSetting IMemberComponentCollection.ProfileSetting { get; }
        [Inject] IMoveSetting IMemberComponentCollection.MoveSetting { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, MemberOwner>
        {

        }
    }
}
