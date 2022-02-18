using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public class EnemySetting : MonoBehaviour, IMoveSetting, IHealthSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;

        [Header("ˆÚ“®‘¬“x")]
        [SerializeField] float _moveSpeed;
        [Header("‰ñ“]‘¬“x")]
        [SerializeField] float _rotateSpeed;
        [Header("Health’l")]
        [SerializeField] int _healthMax;
    }
}
