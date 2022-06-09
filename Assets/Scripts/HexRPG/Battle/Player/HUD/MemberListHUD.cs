using UnityEngine;
using UniRx;
using System.Linq;
using Zenject;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    //TODO: HP=0Ç…Ç»Ç¡ÇΩMemberÇ«Ç§Ç∑ÇÈÅH
    public class MemberListHUD : MonoBehaviour, ICharacterHUD
    {
        BattleData _battleData;

        [SerializeField] GameObject _selectedMember;
        ICharacterHUD _selectedMemberHUD;

        [SerializeField] Transform _allMemberList;

        int _maxMemberCount;

        [Inject]
        public void Construct(BattleData battleData)
        {
            _battleData = battleData;
        }

        void Start()
        {
            _selectedMemberHUD = _selectedMember.GetComponent<ICharacterHUD>();
            _maxMemberCount = _battleData.maxMemberCount;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                for(int i = 0; i < _maxMemberCount; i++)
                {
                    var memberList = playerOwner.MemberObservable.MemberList.ToList();

                    var child = _allMemberList.GetChild(i);
                    if (i >= memberList.Count)
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }
                    child.GetComponent<ICharacterHUD>().Bind(memberList[i]);
                }

                playerOwner.MemberObservable.CurMember
                    .Subscribe(curMember =>
                    {
                        _selectedMemberHUD.Bind(curMember);

                        var memberList = playerOwner.MemberObservable.MemberList.ToList();
                        for(int i = 0; i < _maxMemberCount; i++)
                        {
                            var hud = _allMemberList.GetChild(i).GetComponent<IMemberHUD>();
                            if (hud.IsSelected) hud.IsSelected = false;
                            if (i == memberList.IndexOf(curMember)) hud.IsSelected = true;
                        }
                    })
                    .AddTo(this);

                playerOwner.ActionStateObservable.CurrentState
                    .Where(state => state != null)
                    .Subscribe(state =>
                    {
                        for (int i = 0; i < _maxMemberCount; i++)
                        {
                            if (i >= playerOwner.MemberObservable.MemberList.Count) continue;
                            var type = state.Type;
                            _allMemberList.GetChild(i).GetComponent<IMemberHUD>().SwitchShowBtnChange(
                                type == ActionStateType.IDLE || type == ActionStateType.MOVE || type == ActionStateType.SKILL_SELECT);
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}
