using UnityEngine;
using Zenject;

namespace HexRPG.Battle.Player.Member
{
    public interface IMemberComponentCollection : ICharacterComponentCollection
    {
        IMemberSelectedObservable SelectedObservable { get; }
        IColliderController ColliderController { get; }
        IAnimationController AnimationController { get; }
        ICombatSpawnController CombatSpawnController { get; }
        ICombatSpawnObservable CombatSpawnObservable { get; }
        ICombatController CombatController { get; }
        ISkillSpawnController SkillSpawnController { get; }
        ISkillSpawnObservable SkillSpawnObservable { get; }
        ISkillController SkillController { get; }
        IActiveController ActiveController { get; }
        ISkillPoint SkillPoint { get; }
        IMoveSetting MoveSetting { get; }
    }

    public class MemberOwner : MonoBehaviour, IMemberComponentCollection
    {
        [Inject] IProfileSetting ICharacterComponentCollection.ProfileSetting { get; }
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        [Inject] ITransformController ICharacterComponentCollection.TransformController { get; }
        [Inject] IHealth ICharacterComponentCollection.Health { get; }

        [Inject] IMemberSelectedObservable IMemberComponentCollection.SelectedObservable { get; }
        [Inject] IColliderController IMemberComponentCollection.ColliderController { get; }
        [Inject] IAnimationController IMemberComponentCollection.AnimationController { get; }
        [Inject] ICombatSpawnController IMemberComponentCollection.CombatSpawnController { get; }
        [Inject] ICombatSpawnObservable IMemberComponentCollection.CombatSpawnObservable { get; }
        [Inject] ICombatController IMemberComponentCollection.CombatController { get; }
        [Inject] ISkillSpawnController IMemberComponentCollection.SkillSpawnController { get; }
        [Inject] ISkillSpawnObservable IMemberComponentCollection.SkillSpawnObservable { get; }
        [Inject] ISkillController IMemberComponentCollection.SkillController { get; }
        [Inject] IActiveController IMemberComponentCollection.ActiveController { get; }
        [Inject] ISkillPoint IMemberComponentCollection.SkillPoint { get; }
        [Inject] IMoveSetting IMemberComponentCollection.MoveSetting { get; }

        public class Factory : PlaceholderFactory<Transform, Vector3, MemberOwner>
        {

        }
    }
}
