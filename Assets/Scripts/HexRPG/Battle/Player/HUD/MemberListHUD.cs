using UnityEngine;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    public class MemberListHUD : MonoBehaviour, ICharacterHUD
    {
        void ICharacterHUD.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                // ŠeMemberHud‚ÖBind
                var memberList = playerOwner.MemberObservable.MemberList;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (i > memberList.Length - 1)
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }
                    if(child.TryGetComponent(out ICharacterHUD hud))
                    {
                        hud.Bind(memberList[i]);
                    }
                }
            }
        }
    }
}
