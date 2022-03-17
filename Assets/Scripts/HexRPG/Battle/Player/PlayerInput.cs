using UnityEngine;
using System;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    using Battle.UI;

    public class PlayerInput : MonoBehaviour, ICharacterInput
    {
        IUpdateObservable _updateObservable;

        [Header("スキル選択ボタンリスト")]
        [SerializeField] Transform _skillBtnList;

        [Header("スキル決定ボタン")]
        [SerializeField] GameObject _btnSkillDecide;
        [Header("スキルキャンセルボタン")]
        [SerializeField] GameObject _btnSkillCancel;

        [Header("カメラ回転ボタン")]
        [SerializeField] Transform _cameraRotLeft;
        [SerializeField] Transform _cameraRotRight;

        IReadOnlyReactiveProperty<Vector3> ICharacterInput.Direction => _direction;
        ReactiveProperty<Vector3> _direction = new ReactiveProperty<Vector3>();

        IReadOnlyReactiveProperty<int> ICharacterInput.SelectedSkillIndex => _selectedSkillIndex;
        ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        IObservable<Unit> ICharacterInput.OnSkillDecide => _onSkillDecide;
        ISubject<Unit> _onSkillDecide = new Subject<Unit>();
        IObservable<Unit> ICharacterInput.OnSkillCancel => _onSkillCancel;
        ISubject<Unit> _onSkillCancel = new Subject<Unit>();

        IReadOnlyReactiveProperty<int> ICharacterInput.CameraRotateDir => _cameraRotateDir;
        ReactiveProperty<int> _cameraRotateDir = new ReactiveProperty<int>();

        [Inject]
        public void Construct(IUpdateObservable updateObservable)
        {
            _updateObservable = updateObservable;
        }

        void Start()
        {
            int selectedSkillIndex = -1;
            var isBtnSkillDecideClicked = false;
            var isBtnSkillCancelClicked = false;
            int cameraRotateDir = 0;

            _updateObservable.OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    UpdateDirection();

                    if (selectedSkillIndex != -1)
                    {
                        _selectedSkillIndex.SetValueAndForceNotify(selectedSkillIndex);
                        selectedSkillIndex = -1;
                    }

                    if (isBtnSkillDecideClicked)
                    {
                        _onSkillDecide.OnNext(Unit.Default);
                        isBtnSkillDecideClicked = false;
                    }
                    if (isBtnSkillCancelClicked)
                    {
                        _onSkillCancel.OnNext(Unit.Default);
                        isBtnSkillCancelClicked = false;
                    }

                    if(cameraRotateDir != 0)
                    {
                        _cameraRotateDir.SetValueAndForceNotify(cameraRotateDir);
                        cameraRotateDir = 0;
                    }
                }).AddTo(this);

            void SetSkillBtnClickEvent(GameObject btn, int index)
            {
                btn.OnClickListener(() =>
                {
                    selectedSkillIndex = index;
                }, gameObject);
            }
            for (int i = 0; i < _skillBtnList.childCount; i++)
            {
                SetSkillBtnClickEvent(_skillBtnList.GetChild(i).gameObject, i);
            }

            _btnSkillDecide.OnClickListener(() =>
            {
                isBtnSkillDecideClicked = true;
            }, gameObject);
            _btnSkillCancel.OnClickListener(() =>
            {
                isBtnSkillCancelClicked = true;
            }, gameObject);

            _cameraRotLeft.GetChild(2).gameObject.OnClickListener(() =>
            {
                cameraRotateDir = +1;
            }, gameObject);

            _cameraRotRight.GetChild(2).gameObject.OnClickListener(() =>
            {
                cameraRotateDir = -1;
            }, gameObject);
        }

        void UpdateDirection()
        {
            _direction.Value = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        }
    }
}
