using UnityEngine;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    //TODO: HP=0Ç…Ç»Ç¡ÇΩMemberÇ«Ç§Ç∑ÇÈÅH
    public class MemberListHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] GameObject _selectedMember;
        IMemberHUD _selectedMemberHUD;

        [SerializeField] Transform _standingMemberList;

        void Start()
        {
            _selectedMemberHUD = _selectedMember.GetComponent<IMemberHUD>();
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(curMember =>
                    {
                        _selectedMemberHUD.Bind(curMember);

                        var standingMemberList = playerOwner.MemberObservable.StandingMemberList;
                        for (int i = 0; i < 3; i++)
                        {
                            var child = _standingMemberList.GetChild(3 - 1 - i);
                            if(i >= standingMemberList.Count)
                            {
                                child.gameObject.SetActive(false);
                                continue;
                            }
                            child.GetComponent<IMemberHUD>().Bind(standingMemberList[i]);
                        }
                    })
                    .AddTo(this);

                playerOwner.ActionStateObservable.CurrentState
                    .Where(state => state != null)
                    .Subscribe(state =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (i >= playerOwner.MemberObservable.StandingMemberList.Count) continue;
                            var type = state.Type;
                            _standingMemberList.GetChild(3 - 1 - i).GetComponent<IMemberHUD>().SwitchShowBtnChange(
                                type == ActionStateType.IDLE || type == ActionStateType.MOVE || type == ActionStateType.SKILL_SELECT);
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}
