using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.UI
{
    public class UIManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("CombatのUI実装オブジェクト")]
        [SerializeField] GameObject _combatBtn;
        [Header("SkillのUI実装オブジェクト")]
        [SerializeField] GameObject _skillList;
        [Header("MemberのUI実装オブジェクト")]
        [SerializeField] GameObject _memberList;
        ICharacterUI _combatUI;
        ICharacterUI _skillListUI;
        ICharacterUI _memberListUI;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable
        )
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            if(_combatBtn.TryGetComponent(out _combatUI) && 
                _skillList.TryGetComponent(out _skillListUI) && 
                _memberList.TryGetComponent(out _memberListUI))
            {
                _battleObservable.OnPlayerSpawn
                    .Subscribe(playerOwner =>
                    {
                        new List<ICharacterUI>
                        {
                            _combatUI,
                            _skillListUI,
                            _memberListUI
                        }
                        .ForEach(ui =>
                        {
                            ui.Bind(playerOwner);
                        });
                    });
            }

        }
    }
}
