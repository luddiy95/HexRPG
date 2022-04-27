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

        [Header("名前")]
        [SerializeField] string _name;
        [Header("アイコン")]
        [SerializeField] Sprite _icon;
        [Header("移動速度")]
        [SerializeField] float _moveSpeed;
        [Header("回転速度")]
        [SerializeField] float _rotateSpeed;
        [Header("Health値")]
        [SerializeField] int _healthMax;
        [Header("Mental値")]
        [SerializeField] int _mentalMax;
        [Header("死ぬときのTimeline")]
        [SerializeField] PlayableAsset _dieTimeline;
    }
}
