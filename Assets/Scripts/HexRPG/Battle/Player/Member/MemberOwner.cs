using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    public interface IMemberComponentCollection : ICharacterComponentCollection
    {
        IColliderController ColliderController { get; }
        IAnimationController AnimationController { get; }
        ICombatSpawnObservable CombatSpawnObservable { get; }
        ICombatController CombatController { get; }
        ISkillSpawnController SkillSpawnController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }
        ISkillController SkillController { get; }
        IActiveController ActiveController { get; }
        IMental Mental { get; }
        IProfileSetting ProfileSetting { get; }
        IMoveSetting MoveSetting { get; }
    }

    public class MemberOwner : MonoBehaviour, IMemberComponentCollection
    {
        [Inject] IColliderController IMemberComponentCollection.ColliderController { get; }
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IHealth ICharacterComponentCollection.Health { get; }
        [Inject] IAnimationController IMemberComponentCollection.AnimationController { get; }
        [Inject] ICombatSpawnObservable IMemberComponentCollection.CombatSpawnObservable { get; }
        [Inject] ICombatController IMemberComponentCollection.CombatController { get; }
        [Inject] ISkillSpawnController IMemberComponentCollection.SkillSpawnController { get; }
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
