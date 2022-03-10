
namespace HexRPG.Battle.Player.Member
{
    public class MemberAnimatorBehaviour : AnimatorBehaviour
    {
        void Start()
        {
            if (_animator == null) TryGetComponent(out _animator);
        }
    }
}
