using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    public class MemberSetting : MonoBehaviour, IMoveSetting, IHealthSetting, IMentalSetting, IProfileSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;
        int IMentalSetting.Max => _mentalMax;
        Sprite IProfileSetting.Icon => _icon;

        [Header("移動速度")]
        [SerializeField] float _moveSpeed;
        [Header("回転速度")]
        [SerializeField] float _rotateSpeed;
        [Header("Health値")]
        [SerializeField] int _healthMax;
        [Header("Mental値")]
        [SerializeField] int _mentalMax;
        [Header("アイコン")]
        [SerializeField] Sprite _icon;
    }
}
