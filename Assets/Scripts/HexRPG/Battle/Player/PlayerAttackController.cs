
namespace HexRPG.Battle.Player
{
    public class PlayerAttackController : AbstractAttackController
    {
        BattleData _battleData;
        IMemberObservable _memberObservable;

        public PlayerAttackController(
            BattleData battleData,
            ICharacterComponentCollection owner,
            IMemberObservable memberObservable
        )
        {
            _battleData = battleData;
            _owner = owner;
            _memberObservable = memberObservable;
        }

        protected override void InternalNotifyAttackHit(HitData hitData)
        {
            if (_battleData.hitTypeSkillpointMap.Table.TryGetValue(hitData.HitType, out int getAmount))
            {
                var curMember = _memberObservable.CurMember.Value;
                if (curMember.DieObservable.IsDead.Value == false) curMember.SkillPoint.Update(getAmount);
            }

            base.InternalNotifyAttackHit(hitData);
        }
    }
}
