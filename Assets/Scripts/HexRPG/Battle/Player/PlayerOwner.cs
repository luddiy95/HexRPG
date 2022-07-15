using Zenject;
using UnityEngine;
using UnityEditor;

namespace HexRPG.Battle.Player
{
    public interface IPlayerComponentCollection : IAttackComponentCollection
    {
        IMemberController MemberController { get; }
        IMemberObservable MemberObservable { get; }
        ICharacterActionStateController CharacterActionStateController { get; }
        ISelectSkillObservable SelectSkillObservable { get; }

        //TODO: Decorator—p
        IActionStateObservable ActionStateObservable { get; }
    }

    public class PlayerOwner : AbstractOwner<PlayerOwner>, IPlayerComponentCollection
    {
        IProfileSetting ICharacterComponentCollection.ProfileSetting => MemberOwner.ProfileSetting;
        [Inject] IDieObservable ICharacterComponentCollection.DieObservable { get; }
        IHealth ICharacterComponentCollection.Health => MemberOwner.Health;

        [Inject] IAttackApplicator IAttackComponentCollection.AttackApplicator { get; }
        [Inject] IAttackController IAttackComponentCollection.AttackController { get; }
        [Inject] IAttackObservable IAttackComponentCollection.AttackObservable { get; }
        [Inject] IDamageApplicable IAttackComponentCollection.DamageApplicable { get; }
        [Inject] ILiberateObservable IAttackComponentCollection.LiberateObservable { get; }

        [Inject] IMemberController IPlayerComponentCollection.MemberController { get; }
        [Inject] IMemberObservable IPlayerComponentCollection.MemberObservable { get; }
        [Inject] ICharacterActionStateController IPlayerComponentCollection.CharacterActionStateController { get; }
        [Inject] ISelectSkillObservable IPlayerComponentCollection.SelectSkillObservable { get; }

        //TODO: Decorator—p
        [Inject] IActionStateObservable IPlayerComponentCollection.ActionStateObservable { get; }

        ICharacterComponentCollection MemberOwner => (this as IPlayerComponentCollection).MemberObservable.CurMember.Value;

#if UNITY_EDITOR

        IPlayerComponentCollection _playerOwner => this;

        public void OnInspectorGUI()
        {
            if (GUILayout.Button("Damage"))
            {
                _playerOwner.DamageApplicable.OnHitTest(10);
            }
            if (GUILayout.Button("Die"))
            {
                _playerOwner.DamageApplicable.OnHitTest(_playerOwner.Health.Max);
            }
        }

        [CustomEditor(typeof(PlayerOwner))]
        public class EnemyOwnerInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((PlayerOwner)target).OnInspectorGUI();
            }
        }

#endif
    }
}
