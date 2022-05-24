using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Enemy
{
    public class EnemySetting : MonoBehaviour, IMoveSetting, IHealthSetting, IDieSetting, IProfileSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;
        PlayableAsset IDieSetting.Timeline => _dieTimeline;
        string IProfileSetting.Name => _name;
        Sprite IProfileSetting.Icon => _icon;
        Attribute IProfileSetting.Attribute => _attribute;

        [Header("���O")]
        [SerializeField] string _name;
        [Header("�A�C�R��")]
        [SerializeField] Sprite _icon;
        [Header("����")]
        [SerializeField] Attribute _attribute;
        [Header("�ړ����x")]
        [SerializeField] float _moveSpeed;
        [Header("��]���x")]
        [SerializeField] float _rotateSpeed;
        [Header("Health�l")]
        [SerializeField] int _healthMax;
        [Header("���ʂƂ���Timeline")]
        [SerializeField] PlayableAsset _dieTimeline;
    }
}
