using UnityEngine;
using UnityEditor;
using System;
using Zenject;

namespace HexRPG.Battle.Stage.Tower
{
    public interface ITowerComponentCollection : ICharacterComponentCollection
    {
        ITowerController TowerController { get; }
        ITowerObservable TowerObservable { get; }
        IEnemySpawnObservable EnemySpawnObservable { get; }

        //TODO: Inspector—p
        IDamageApplicable DamageApplicable { get; }
    }

    public class TowerOwner : MonoBehaviour, ITowerComponentCollection
    {
        [Inject] IProfileSetting ICharacterComponentCollection.ProfileSetting { get; }
        IDieObservable ICharacterComponentCollection.DieObservable => throw new NullReferenceException();
        [Inject] IHealth ICharacterComponentCollection.Health { get; }

        [Inject] ITransformController IBaseComponentCollection.TransformController { get; }
        [Inject] ITowerController ITowerComponentCollection.TowerController { get; }
        [Inject] ITowerObservable ITowerComponentCollection.TowerObservable { get; }
        [Inject] IEnemySpawnObservable ITowerComponentCollection.EnemySpawnObservable { get; }

        //TODO: Inspector—p
        [Inject] IDamageApplicable ITowerComponentCollection.DamageApplicable { get; }

#if UNITY_EDITOR

        ITowerComponentCollection _towerOwner => this;

        public void OnInspectorGUI()
        {
            if (GUILayout.Button("Damage"))
            {
                _towerOwner.DamageApplicable.OnHitTest(10);
            }
            if (GUILayout.Button("Die"))
            {
                _towerOwner.DamageApplicable.OnHitTest(_towerOwner.Health.Max);
            }
        }

        [CustomEditor(typeof(TowerOwner))]
        public class EnemyOwnerInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ((TowerOwner)target).OnInspectorGUI();
            }
        }

#endif
    }
}
