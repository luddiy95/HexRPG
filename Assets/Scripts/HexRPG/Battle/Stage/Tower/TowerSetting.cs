using UnityEngine;

namespace HexRPG.Battle.Stage.Tower
{
    public class TowerSetting : MonoBehaviour, IHealthSetting, IProfileSetting
    {
        int IHealthSetting.Max => _healthMax;
        string IProfileSetting.Name => _name;
        Sprite IProfileSetting.Icon => _icon;
        Attribute IProfileSetting.Attribute => _attribute;

        [Header("Health値")]
        [SerializeField] int _healthMax;
        [Header("名前")]
        [SerializeField] string _name;
        [Header("アイコン")]
        [SerializeField] Sprite _icon;
        [Header("属性")]
        [SerializeField] Attribute _attribute;
    }
}
