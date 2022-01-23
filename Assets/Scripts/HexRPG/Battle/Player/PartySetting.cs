using UnityEngine;

namespace HexRPG.Battle.Player
{
    public class PartySetting : AbstractCustomComponentBehaviour, IPartySetting
    {
        GameObject[] IPartySetting.Party => _party;

        [Header("�p�[�e�B�̃����o�[���X�g")]
        [SerializeField] GameObject[] _party;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IPartySetting>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}