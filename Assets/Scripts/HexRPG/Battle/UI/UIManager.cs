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
        ICharacterUI _combatUI;
        ICharacterUI _skillListUI;

        [Inject]
        public void Construct(
            IBattleObservable battleObservable
        )
        {
            _battleObservable = battleObservable;
        }

        void Start()
        {
            if (_combatBtn.TryGetComponent(out _combatUI) &&
                _skillList.TryGetComponent(out _skillListUI))
            {
                _battleObservable.OnPlayerSpawn
                    .Skip(1)
                    .Subscribe(playerOwner =>
                    {
                        new List<ICharacterUI>
                        {
                            _combatUI,
                            _skillListUI
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
