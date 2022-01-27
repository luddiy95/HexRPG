using UnityEngine;

namespace HexRPG.Battle.Enemy
{
    public class EnemySetting : AbstractCustomComponentBehaviour, IMoveSetting, IHealthSetting
    {
        float IMoveSetting.MoveSpeed => _moveSpeed;
        float IMoveSetting.RotateSpeed => _rotateSpeed;
        int IHealthSetting.Max => _healthMax;

        [Header("ˆÚ“®‘¬“x")]
        [SerializeField] float _moveSpeed;
        [Header("‰ñ“]‘¬“x")]
        [SerializeField] float _rotateSpeed;
        [Header("Health’l")]
        [SerializeField] int _healthMax;

        public override void Register(ICustomComponentCollection owner)
        {
            base.Register(owner);

            owner.RegisterInterface<IMoveSetting>(this);
            owner.RegisterInterface<IHealthSetting>(this);
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
