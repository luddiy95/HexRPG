using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;

namespace HexRPG.Battle.UI
{
    public class UIManager : MonoBehaviour
    {
        IBattleObservable _battleObservable;

        [Header("Skill��UI�����I�u�W�F�N�g")]
        [SerializeField] GameObject _skillList;
        [Header("Member��UI�����I�u�W�F�N�g")]
        [SerializeField] GameObject _memberList;
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
            _skillListUI = _skillList.GetComponent<ICharacterUI>();
            _memberListUI = _memberList.GetComponent<ICharacterUI>();

            _battleObservable.OnPlayerSpawn
                .Skip(1)
                .Subscribe(playerOwner =>
                {
                    new List<ICharacterUI>
                    {
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
