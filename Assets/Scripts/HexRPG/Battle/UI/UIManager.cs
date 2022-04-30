using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.UI
{
    public class UIManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("Combat��UI�����I�u�W�F�N�g")]
        [SerializeField] GameObject _combatBtn;
        [Header("Skill��UI�����I�u�W�F�N�g")]
        [SerializeField] GameObject _skillList;
        [Header("Member��UI�����I�u�W�F�N�g")]
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
