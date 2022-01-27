using UnityEngine;
using System;

namespace HexRPG.Battle
{
    using Stage;

    public interface ISpawnSettings : IFeature
    {
        SpawnSetting PlayerSpawnSetting { get; }
        SpawnSetting[] EnemySpawnSettings { get; }
    }

    [Serializable]
    public class SpawnSetting
    {
        public GameObject Prefab => _prefab;
        [SerializeField] GameObject _prefab;

        public Hex SpawnHex => _spawnHex;
        [SerializeField] Hex _spawnHex;
    }

    public class SpawnSettings : AbstractCustomComponentBehaviour, ISpawnSettings
    {
        SpawnSetting ISpawnSettings.PlayerSpawnSetting => _playerSpawnSetting;
        [Header("Player�����ݒ�")]
        [SerializeField] SpawnSetting _playerSpawnSetting;

        SpawnSetting[] ISpawnSettings.EnemySpawnSettings => _enemySpawnSettings;
        [Header("Enemy�����ݒ�")]
        [SerializeField] SpawnSetting[] _enemySpawnSettings;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<ISpawnSettings>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
