using UnityEngine;

namespace HexRPG.Battle.Stage
{
    public class TowerSetting : MonoBehaviour, IHealthSetting
    {
        int IHealthSetting.Max => _healthMax;
        [Header("Health’l")]
        [SerializeField] int _healthMax;
    }
}
