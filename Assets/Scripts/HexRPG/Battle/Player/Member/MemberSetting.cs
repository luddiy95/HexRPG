using UnityEngine;

namespace HexRPG.Battle.Player.Member
{
    using Skill;

    public class MemberSetting : AbstractCustomComponentBehaviour, IMoveSetting, IHealthSetting, IMentalSetting, IProfileSetting, ISkillListSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;
        int IMentalSetting.Max => _mentalMax;
        Sprite IProfileSetting.StatusIcon => _statusIcon;
        Sprite IProfileSetting.OptionIcon => _optionIcon;
        GameObject[] ISkillListSetting.SkillList => _skillList;

        [Header("�ړ����x")]
        [SerializeField] float _moveSpeed;
        [Header("��]���x")]
        [SerializeField] float _rotateSpeed;
        [Header("Health�l")]
        [SerializeField] int _healthMax;
        [Header("Mental�l")]
        [SerializeField] int _mentalMax;
        [Header("�X�e�[�^�X�A�C�R��")]
        [SerializeField] Sprite _statusIcon;
        [Header("�I�v�V�����A�C�R��")]
        [SerializeField] Sprite _optionIcon;
        [Header("�X�L�����X�g")]
        [SerializeField] GameObject[] _skillList;

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
