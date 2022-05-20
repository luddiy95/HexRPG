using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace HexRPG.Battle.Player.UI
{
    using Battle.UI;

    public class CombatUI : MonoBehaviour, ICharacterUI
    {
        [SerializeField] Image _background;
        [SerializeField] Image _icon;

        [SerializeField] Sprite _combatEnableBackgroundSprite;
        [SerializeField] Sprite _combatDisableBackgroundSprite;
        [SerializeField] Material _combatEnableMaterial;
        [SerializeField] Material _combatDisableMaterial;

        void ICharacterUI.Bind(ICharacterComponentCollection chara)
        {
            if (chara is IPlayerComponentCollection playerOwner)
            {
                playerOwner.MemberObservable.CurMember
                    .Subscribe(memberOwner =>
                    {
                        var combatIcon = memberOwner.CombatSpawnObservable.Combat.CombatSetting.Icon;

                        _icon.sprite = combatIcon;
                    })
                    .AddTo(this);

                playerOwner.ActionStateObservable.CurrentState
                    .Where(state => state != null)
                    .Subscribe(state =>
                    {
                        switch (state.Type)
                        {
                            case ActionStateType.IDLE:
                            case ActionStateType.MOVE:
                            case ActionStateType.COMBAT:
                            case ActionStateType.SKILL_SELECT:
                                _background.sprite = _combatEnableBackgroundSprite;
                                _icon.material = _combatEnableMaterial;
                                break;
                            default:
                                _background.sprite = _combatDisableBackgroundSprite;
                                _icon.material = _combatDisableMaterial;
                                break;
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}
