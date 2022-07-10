using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    public class MemberListHUD : MonoBehaviour, ICharacterHUD
    {
        MemberStatusHUD.Factory _memberStatusFactory;

        [SerializeField] GameObject _selectedMember;
        ICharacterHUD _selectedMemberHUD;

        [SerializeField] Transform _memberListRoot;
        List<IMemberHUD> _memberList = new List<IMemberHUD>(8);

        [Inject]
        public void Construct(
            MemberStatusHUD.Factory memberStatusFactory
        )
        {
            _memberStatusFactory = memberStatusFactory;
        }

        void Awake()
        {
            _selectedMemberHUD = _selectedMember.GetComponent<ICharacterHUD>();
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                var memberList = playerOwner.MemberObservable.MemberList;
                memberList.ForEach(member =>
                {
                    var clone = _memberStatusFactory.Create();
                    clone.transform.SetParent(_memberListRoot);
                    var hud = clone.GetComponent<IMemberHUD>();
                    _memberList.Add(hud);
                    hud.Bind(member);
                });

                playerOwner.MemberObservable.CurMember
                    .Subscribe(curMember =>
                    {
                        _selectedMemberHUD.Bind(curMember);

                        for(int i = 0; i < _memberList.Count; i++)
                        {
                            var hud = _memberList[i];
                            if (hud.IsSelected) hud.IsSelected = false;
                            if (i == playerOwner.MemberObservable.MemberList.IndexOf(curMember)) hud.IsSelected = true;
                        }
                    })
                    .AddTo(this);

                playerOwner.ActionStateObservable.CurrentState
                    .Subscribe(state =>
                    {
                        _memberList.ForEach(hud =>
                        {
                            var type = state.Type;
                            hud.SwitchShowBtnChange(type == ActionStateType.IDLE || type == ActionStateType.MOVE || type == ActionStateType.SKILL_SELECT);
                        });
                    })
                    .AddTo(this);
            }
        }
    }
}
