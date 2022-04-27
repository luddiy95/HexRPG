using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Member
{
    public class MemberSetting : MonoBehaviour, IMoveSetting, IHealthSetting, IMentalSetting, IDieSetting, IProfileSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;
        int IMentalSetting.Max => _mentalMax;
        PlayableAsset IDieSetting.Timeline => _dieTimeline;
        string IProfileSetting.Name => _name;
        Sprite IProfileSetting.Icon => _icon;

        [Header("���O")]
        [SerializeField] string _name;
        [Header("�A�C�R��")]
        [SerializeField] Sprite _icon;
        [Header("�ړ����x")]
        [SerializeField] float _moveSpeed;
        [Header("��]���x")]
        [SerializeField] float _rotateSpeed;
        [Header("Health�l")]
        [SerializeField] int _healthMax;
        [Header("Mental�l")]
        [SerializeField] int _mentalMax;
        [Header("���ʂƂ���Timeline")]
        [SerializeField] PlayableAsset _dieTimeline;
    }
}
