using UnityEngine;
using UnityEngine.Playables;

namespace HexRPG.Battle.Player.Member
{
    public class MemberSetting : MonoBehaviour, IMoveSetting, IHealthSetting, ISkillPointSetting, IDieSetting, IProfileSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;
        int ISkillPointSetting.Max => _skillPointMax;
        PlayableAsset IDieSetting.Timeline => _dieTimeline;
        string IProfileSetting.Name => _name;
        Sprite IProfileSetting.Icon => _icon;
        Attribute IProfileSetting.Attribute => _attribute;

        [Header("名前")]
        [SerializeField] string _name;
        [Header("アイコン")]
        [SerializeField] Sprite _icon;
        [Header("属性")]
        [SerializeField] Attribute _attribute;
        [Header("移動速度")]
        [SerializeField] float _moveSpeed;
        [Header("回転速度")]
        [SerializeField] float _rotateSpeed;
        [Header("Health値")]
        [SerializeField] int _healthMax;
        [Header("SkillPoint")]
        [SerializeField] int _skillPointMax;
        [Header("死ぬときのTimeline")]
        [SerializeField] PlayableAsset _dieTimeline;
    }
}
