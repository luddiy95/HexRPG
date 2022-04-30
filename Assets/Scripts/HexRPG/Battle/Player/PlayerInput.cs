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

        [Header("通常攻撃ボタン")]
        [SerializeField] GameObject _btnCombat;

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
        readonly ReactiveProperty<Vector3> _direction = new ReactiveProperty<Vector3>();

        IObservable<Unit> ICharacterInput.OnCombat => _onCombat;
        readonly ISubject<Unit> _onCombat = new Subject<Unit>();

        IReadOnlyReactiveProperty<int> ICharacterInput.SelectedSkillIndex => _selectedSkillIndex;
        readonly ReactiveProperty<int> _selectedSkillIndex = new ReactiveProperty<int>(-1);

        IObservable<Unit> ICharacterInput.OnSkillDecide => _onSkillDecide;
        readonly ISubject<Unit> _onSkillDecide = new Subject<Unit>();
        IObservable<Unit> ICharacterInput.OnSkillCancel => _onSkillCancel;
        readonly ISubject<Unit> _onSkillCancel = new Subject<Unit>();

        IObservable<int> ICharacterInput.CameraRotateDir => _cameraRotateDir;
        readonly ISubject<int> _cameraRotateDir = new Subject<int>();

        [Inject]
        public void Construct(IUpdateObservable updateObservable)
        {
            _updateObservable = updateObservable;
        }

        void Start()
        {
            var isBtnCombatClicked = false;
            int selectedSkillIndex = -1;
            var isBtnSkillDecideClicked = false;
            var isBtnSkillCancelClicked = false;
            int cameraRotateDir = 0;

            _updateObservable.OnUpdate((int)UPDATE_ORDER.INPUT)
                .Subscribe(_ =>
                {
                    UpdateDirection();

                    //! 移動中に操作割り込みしたいものはUpdateDirectionより下に記述する
                    if (selectedSkillIndex != -1)
                    {
                        _selectedSkillIndex.SetValueAndForceNotify(selectedSkillIndex);
                        selectedSkillIndex = -1;
                    }

                    if (isBtnCombatClicked)
                    {
                        _onCombat.OnNext(Unit.Default);
                        isBtnCombatClicked = false;
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
                        _cameraRotateDir.OnNext(cameraRotateDir);
                        cameraRotateDir = 0;
                    }
                }).AddTo(this);

            _btnCombat.OnClickListener(() =>
            {
                isBtnCombatClicked = true;
            }, gameObject);

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
            _direction.SetValueAndForceNotify(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")));
        }
    }
}
