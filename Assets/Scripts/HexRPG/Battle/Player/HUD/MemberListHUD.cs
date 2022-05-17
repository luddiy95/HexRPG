using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;
    using Member;

    public class MemberListHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] GameObject _selectedMember;
        ICharacterHUD _selectedMemberHUD;

        [SerializeField] Transform _memberList;

        void Start()
        {
            _selectedMemberHUD = _selectedMember.GetComponent<ICharacterHUD>();
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                var memberList = playerOwner.MemberObservable.MemberList;

                playerOwner.MemberObservable.CurMember
                    .Subscribe(curMember =>
                    {
                        _selectedMemberHUD.Bind(curMember);

                        var standingMemberList = new List<IMemberComponentCollection>();
                        Array.ForEach(memberList, member =>
                        {
                            if (member != curMember) standingMemberList.Add(member);
                        });

                        for(int i = 0; i < 3; i++)
                        {
                            var child = _memberList.GetChild(3 - 1 - i);
                            if(i >= standingMemberList.Count)
                            {
                                child.gameObject.SetActive(false);
                                continue;
                            }
                            child.GetComponent<ICharacterHUD>().Bind(standingMemberList[i]);
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}
