using UnityEngine;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    public class MemberListHUD : MonoBehaviour, ICharacterHUD
    {
        [SerializeField] Transform _memberStatusList;

        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                var memberList = playerOwner.MemberObservable.MemberList;

                for (int i = 0; i < _memberStatusList.childCount; i++)
                {
                    if (i > memberList.Length - 1) continue;
                    if(_memberStatusList.GetChild(i).TryGetComponent(out ICharacterHUD hud))
                    {
                        hud.Bind(memberList[i]);
                    }
                }
            }
        }
    }
}
