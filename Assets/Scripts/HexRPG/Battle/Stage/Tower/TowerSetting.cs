using UnityEngine;

namespace HexRPG.Battle.Stage.Tower
{
    public class TowerSetting : MonoBehaviour, IHealthSetting, IProfileSetting
    {
        int IHealthSetting.Max => _healthMax;
        string IProfileSetting.Name => _name;
        Sprite IProfileSetting.Icon => _icon;
        Attribute IProfileSetting.Attribute => _attribute;

        [Header("Health�l")]
        [SerializeField] int _healthMax;
        [Header("���O")]
        [SerializeField] string _name;
        [Header("�A�C�R��")]
        [SerializeField] Sprite _icon;
        [Header("����")]
        [SerializeField] Attribute _attribute;
    }
}
