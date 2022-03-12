using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    public class MemberSetting : MonoBehaviour, IMoveSetting, IHealthSetting, IMentalSetting, IProfileSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;
        int IMentalSetting.Max => _mentalMax;
        //TODO: ����
        Sprite IProfileSetting.StatusIcon => _statusIcon;
        Sprite IProfileSetting.Icon => _icon;

        [Header("�ړ����x")]
        [SerializeField] float _moveSpeed;
        [Header("��]���x")]
        [SerializeField] float _rotateSpeed;
        [Header("Health�l")]
        [SerializeField] int _healthMax;
        [Header("Mental�l")]
        [SerializeField] int _mentalMax;
        //TODO: ����
        [Header("�X�e�[�^�X�A�C�R��")]
        [SerializeField] Sprite _statusIcon;
        [Header("�A�C�R��")]
        [SerializeField] Sprite _icon;
    }
}
