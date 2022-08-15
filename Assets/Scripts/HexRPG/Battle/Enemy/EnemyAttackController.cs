
namespace HexRPG.Battle.Enemy
{
    public class EnemyAttackController : AbstractAttackController
    {
        public EnemyAttackController(
            ICharacterComponentCollection owner
        )
        {
            _owner = owner;
        }
    }
}
