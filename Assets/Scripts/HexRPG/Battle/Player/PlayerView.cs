using System.Collections.Generic;
using UnityEngine;
using UniRx;
using DG.Tweening;
using System;

namespace HexRPG.Battle.Player
{
    using Stage;
    using Character;
    using static ActionPanelStatus;

    public class PlayerModel : ICharacterSkillCallback
    {
        public PlayerModel(IPlayerTransformProvider playerTransformProvider)
        {
            // Player
            _status = Status.IDLE;
            _playerTransformProvider = playerTransformProvider;
        }

        public void Init(List<Character.Character> characterList)
        {
            _characterList = characterList;

            _initCharacters.OnNext(Unit.Default);
            _curCharacter.Value = _characterList[0];
        }

        #region Player
        enum Status
        {
            IDLE,
            MOVE
        }
        Status _status;

        readonly string HEX_MOVEABLE_INDICATOR = "HexMoveableIndicator";

        IPlayerTransformProvider _playerTransformProvider;

        public IReadOnlyReactiveProperty<Hex> HexMoveTo => _hexMoveTo;
        readonly ReactiveProperty<Hex> _hexMoveTo = new ReactiveProperty<Hex>();

        public Hex LandedHex => BattlePreference.Instance.GetAnyLandedHex(_playerTransformProvider.Transform.position);

        public IObservable<Unit> UpdateMoveableIndicator => _updateMoveableIndicator;
        readonly Subject<Unit> _updateMoveableIndicator = new Subject<Unit>();

        public void OnScreenTouched(Vector3 pos)
        {
            if(_status == Status.IDLE && ActionPanelStatus == CLOSE)
            {
                Ray ray = Camera.main.ScreenPointToRay(pos);
                Physics.Raycast(ray, out var hit, Mathf.Infinity, 1 << LayerMask.NameToLayer(HEX_MOVEABLE_INDICATOR));
                if (hit.collider == null) return;
                Hex hex = BattlePreference.Instance.GetAnyLandedHex(hit.transform.position);
                if (hex == null) return;
                _hexMoveTo.Value = hex; // �ړ���animator�J��
            }
        }

        public void OnStartMoveToHex() => _status = Status.MOVE;

        public void OnFinishMoveToHex() => _status = Status.IDLE;
        #endregion

        #region Character
        List<Character.Character> _characterList = new List<Character.Character>();
        public List<Character.Character> CharacterList => _characterList;

        public IObservable<Unit> InitCharacters => _initCharacters;
        readonly Subject<Unit> _initCharacters = new Subject<Unit>();

        public IReadOnlyReactiveProperty<Character.Character> CurCharacter => _curCharacter;
        readonly ReactiveProperty<Character.Character> _curCharacter = new ReactiveProperty<Character.Character>();

        public List<Vector2> CurCharacterSkillRange(int index) => _curCharacter.Value.SkillList[index].Range;

        public IReadOnlyReactiveProperty<string> SkillAnimationParam => _skillAnimationParam;
        readonly ReactiveProperty<string> _skillAnimationParam = new ReactiveProperty<string>();

        void ICharacterSkillCallback.StartSkillAnimation(string animationParam) => _skillAnimationParam.SetValueAndForceNotify(animationParam);

        public IObservable<Unit> LiberateHex => _liberateHex;
        readonly Subject<Unit> _liberateHex = new Subject<Unit>();

        public void ChangeCharacter(int index)
        {

        }

        public void OnStartSkill() => _curCharacterMP.Value = _curCharacter.Value.MP;

        public void OnFinishSkill()
        {
            Debug.Log("finish skill");
            ActionPanelStatus = CLOSE;
            _liberateHex.OnNext(Unit.Default);
            _updateMoveableIndicator.OnNext(Unit.Default);
        }
        #endregion

        #region SelectSkillPanel, SelectCharacterPanel
        ActionPanelStatus _actionPanelStatus = CLOSE;
        ActionPanelStatus ActionPanelStatus
        {
            get { return _actionPanelStatus; }
            set
            {
                _actionPanelStatus = value;
                switch (_actionPanelStatus)
                {
                    case CLOSE:
                        SelectedSkillIndex = -1;
                        SelectedCharacterIndex = -1;
                        _closeSelectSkillPanel.OnNext(Unit.Default);
                        break;
                    case SELECT_SKILL:
                        SelectedCharacterIndex = -1;
                        _openSelectSkillPanel.OnNext(Unit.Default);
                        break;
                    case SELECT_CHARACTER:
                        SelectedSkillIndex = -1;
                        _openSelectCharacterPanel.OnNext(Unit.Default);
                        break;
                }
            }
        }

        public IObservable<Unit> CloseSelectSkillPanel => _closeSelectSkillPanel;
        readonly Subject<Unit> _closeSelectSkillPanel = new Subject<Unit>();

        public IObservable<Unit> OpenSelectSkillPanel => _openSelectSkillPanel;
        readonly Subject<Unit> _openSelectSkillPanel = new Subject<Unit>();

        public IReadOnlyReactiveProperty<int> CurSelectedSkillIndex => _curSelectedSkillIndex;
        readonly ReactiveProperty<int> _curSelectedSkillIndex = new ReactiveProperty<int>();

        public IObservable<Unit> ClearSelectedSkillIndex => _clearSelectedSkillIndex;
        readonly Subject<Unit> _clearSelectedSkillIndex = new Subject<Unit>();

        int _selectedSkillIndex = -1;
        int SelectedSkillIndex
        {
            get { return _selectedSkillIndex; }
            set
            {
                if (value != _selectedSkillIndex || value == -1)
                {
                    DuplicateSelectedCount = 0;
                    _clearSelectedSkillIndex.OnNext(Unit.Default);
                }
                else
                {
                    ++DuplicateSelectedCount;
                }
                if (value != -1) _curSelectedSkillIndex.SetValueAndForceNotify(value);

                _selectedSkillIndex = value;
            }
        }

        public IObservable<Unit> CloseSelectCharacterPanel => _closeSelectCharacterPanel;
        readonly Subject<Unit> _closeSelectCharacterPanel = new Subject<Unit>();

        public IObservable<Unit> OpenSelectCharacterPanel => _openSelectCharacterPanel;
        readonly Subject<Unit> _openSelectCharacterPanel = new Subject<Unit>();

        public IReadOnlyReactiveProperty<int> CurSelectedCharacterIndex => _curSelectedCharacterIndex;
        readonly ReactiveProperty<int> _curSelectedCharacterIndex = new ReactiveProperty<int>();

        public IObservable<Unit> ClearSelectedCharacterIndex => _clearSelectedCharacterIndex;
        readonly Subject<Unit> _clearSelectedCharacterIndex = new Subject<Unit>();

        int _selectedCharacterIndex = -1;
        int SelectedCharacterIndex
        {
            get { return _selectedCharacterIndex; }
            set
            {
                if (value != _selectedCharacterIndex || value == -1) _clearSelectedCharacterIndex.OnNext(Unit.Default);
                if (value != -1) _curSelectedCharacterIndex.Value = value;
                _selectedCharacterIndex = value;
            }
        }

        public int DuplicateSelectedCount { get; private set; } = 0;

        public void OnDecideBtnTouched()
        {
            switch (ActionPanelStatus)
            {
                case CLOSE:
                    if(_status == Status.IDLE) ActionPanelStatus = SELECT_SKILL;
                    break;
                case SELECT_SKILL:
                    if (SelectedSkillIndex == -1) break;
                    if(_curCharacter.Value.TryExecuteSkill(SelectedSkillIndex)) SelectedSkillIndex = -1;
                    break;
            }
        }

        public void OnBackBtnTouched()
        {
            switch (ActionPanelStatus)
            {
                case SELECT_SKILL:
                    ActionPanelStatus = CLOSE;
                    break;
                case SELECT_CHARACTER:
                    ActionPanelStatus = SELECT_SKILL;
                    break;
            }
        }

        public void OnChangeCharacterBtnTouched()
        {
            switch (ActionPanelStatus)
            {
                case SELECT_SKILL: ActionPanelStatus = SELECT_CHARACTER; break;
            }
        }

        public void TrySaveSelectedSkillIndex(int index)
        {
            if (index > _curCharacter.Value.SkillList.Count - 1) return;
            SelectedSkillIndex = index;
        }

        public void TryUpdateSelectedCharacterIndex(int index)
        {
            if (index > _characterList.Count - 1) return;
            SelectedCharacterIndex = index;
        }
        #endregion

        #region StatusPanel
        public IReadOnlyReactiveProperty<int> CurCharacterHP => _curCharacterHP;
        readonly ReactiveProperty<int> _curCharacterHP = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> CurCharacterMP => _curCharacterMP;
        readonly ReactiveProperty<int> _curCharacterMP = new ReactiveProperty<int>();
        #endregion
    }

    public interface IPlayerTransformProvider
    {
        Transform Transform { get; }
    }

    [RequireComponent(typeof(PlayerPresenter))]
    public class PlayerView : MonoBehaviour, IPlayerTransformProvider
    {
        #region Player
        Transform IPlayerTransformProvider.Transform => transform;

        public void SetRotation(float angle) => _characterRoot.rotation = Quaternion.Euler(0, 30 + angle, 0);

        public void ResetRotation() => _characterRoot.rotation = Quaternion.Euler(0, 30, 0);
        #endregion

        #region Character
        [SerializeField] Transform _characterRoot;

        public Character.Character InstantiateCharacter(Character.Character prefab)
        {
            return Instantiate(prefab, _characterRoot);
        }
        #endregion

        #region Animation
        public Animator Animator { get; private set; }

        public void SetAnimator(Animator animator) => Animator = animator;

        public void ResetSpeed()
        {
            Animator.SetFloat(SPEED_HORIZONTAL, 0f);
            Animator.SetFloat(SPEED_VERTICAL, 0f);
        }

        public void ResetTrigger(string trigger)
        {
            Animator.ResetTrigger(trigger);
        }

        public void SetTrigger(string trigger)
        {
            Animator.SetTrigger(trigger);
        }
        #endregion

        #region Hex
        readonly int SPEED_HORIZONTAL = Animator.StringToHash("SpeedHorizontal");
        readonly int SPEED_VERTICAL = Animator.StringToHash("SpeedVertical");

        [SerializeField] Transform moveableIndicatorRoot;

        public Hex LandedHex => BattlePreference.Instance.GetAnyLandedHex(transform.position);

        public void MoveToHex(Hex hex)
        {
            Hex landedHex = LandedHex;
            if (landedHex == null) return;
            Vector3 relativePos = hex.transform.position - landedHex.transform.position;
            relativePos.y = 0;
            Quaternion relativeRot = Quaternion.LookRotation(relativePos, Vector3.up);
            int relativeRotAngle = (int)relativeRot.eulerAngles.y;
            float speedHorizontal = 0f, speedVertical = 0f;
            if (0 < relativeRotAngle && relativeRotAngle < 60)
            {
                speedVertical = 1f;
            }
            else if (60 < relativeRotAngle && relativeRotAngle < 120)
            {
                speedVertical = 1f; speedHorizontal = 1f;
            }
            else if (120 < relativeRotAngle && relativeRotAngle < 180)
            {
                speedVertical = -1f; speedHorizontal = 1f;
            }
            else if (180 < relativeRotAngle && relativeRotAngle < 240)
            {
                speedVertical = -1f;
            }
            else if (240 < relativeRotAngle && relativeRotAngle < 300)
            {
                speedVertical = -1f; speedHorizontal = -1f;
            }
            else if (300 < relativeRotAngle && relativeRotAngle < 360)
            {
                speedVertical = 1f; speedHorizontal = -1f;
            }

            Animator.SetFloat(SPEED_HORIZONTAL, speedHorizontal);
            Animator.SetFloat(SPEED_VERTICAL, speedVertical);

            transform.DOMove(hex.transform.position, 0.5f);
            moveableIndicatorRoot.gameObject.SetActive(false);
        }

        public void UpdateMoveableIndicator()
        {
            for (int i = 0; i < moveableIndicatorRoot.childCount; i++)
            {
                Transform indicator = moveableIndicatorRoot.GetChild(i);
                Hex landedHex = BattlePreference.Instance.GetAnyLandedHex(indicator.position);
                if (landedHex == null) indicator.gameObject.SetActive(false);
                else indicator.gameObject.SetActive(landedHex.IsPlayerHex);
            }

            moveableIndicatorRoot.gameObject.SetActive(true);
        }
        #endregion
    }
}
