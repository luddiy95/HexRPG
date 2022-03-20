using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace HexRPG.Battle.Player.UI
{
    using Battle.UI;

    public class CombatUI : MonoBehaviour, ICharacterUI
    {
        [SerializeField] Transform _combatBtn;

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(memberOwner =>
                    {
                        var combatIcon = memberOwner.CombatSpawnObservable.Combat.CombatSetting.Icon;

                        UpdateBtnIcon(combatIcon);
                    })
                    .AddTo(this);

                //TODO: Player‚ªDamaged‚âSkillÀs’†‚Ì‚Æ‚«‚ÍDisable‚É‚·‚é
            }
        }

        #region View

        void UpdateBtnIcon(Sprite combatIcon)
        {
            _combatBtn.GetChild(0).GetComponent<Image>().sprite = combatIcon;
        }

        void SwitchBtnEnable(bool enable)
        {

        }

        #endregion
    }
}
