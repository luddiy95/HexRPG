using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Skill;

    public class MemberSetting : AbstractCustomComponentBehaviour, IMoveSetting, IHealthSetting, IMentalSetting, IProfileSetting, ISkillListSetting
    {
        float IMoveSetting.Speed => _speed;
        int IHealthSetting.Max => _healthMax;
        int IMentalSetting.Max => _mentalMax;
        Sprite IProfileSetting.StatusIcon => _statusIcon;
        Sprite IProfileSetting.OptionIcon => _optionIcon;
        BaseSkill[] ISkillListSetting.SkillList => _skillList;

        [Header("移動速度")]
        [SerializeField] float _speed;
        [Header("Health値")]
        [SerializeField] int _healthMax;
        [Header("Mental値")]
        [SerializeField] int _mentalMax;
        [Header("ステータスアイコン")]
        [SerializeField] Sprite _statusIcon;
        [Header("オプションアイコン")]
        [SerializeField] Sprite _optionIcon;
        [Header("スキルリスト")]
        [SerializeField] BaseSkill[] _skillList;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMoveSetting>(this);
            owner.RegisterInterface<IHealthSetting>(this);
            owner.RegisterInterface<IMentalSetting>(this);
            owner.RegisterInterface<IProfileSetting>(this);
            owner.RegisterInterface<ISkillListSetting>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
