
namespace HexRPG.Battle.Enemy
{
    public class EnemyAnimatorBehaviour : AnimatorBehaviour
    {
        void Start()
        {
            if (_animator == null) TryGetComponent(out _animator);
        }
    }
}