using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using System;

namespace HexRPG.Battle.Player
{
    using Character;

    [RequireComponent(typeof(PlayerView))]
    public class PlayerPresenter : MonoBehaviour
    {
        readonly string MOVE = "Move";

        PlayerView _view;
        public PlayerModel Model { get; private set; }

        IInputEventProvider _inputEventProvider;

#nullable enable
        IDisposable? _moveEnterSubject;
        IDisposable? _moveExitSubject;
#nullable disable

#nullable enable
        IDisposable? _skillEnterSubject;
        IDisposable? _skillExitSubject;
#nullable disable

        public void Init(List<CharacterData> characterDataList)
        {
            _view = GetComponent<PlayerView>();

            _inputEventProvider = GetComponent<IInputEventProvider>();

            Model = new PlayerModel(_view);
            List<Character.Character> characterList = new List<Character.Character>();
            foreach (var data in characterDataList)
            {
                Character.Character character = _view.InstantiateCharacter(data.CharacterPrefab);
                character.Init(data, Model);
                characterList.Add(character);
            }
            Model.Init(characterList);

            SubscribeTouchEvent();
            SubscribeCharacterChange();
            SubscribeMove();
            SubscribeSkillAnimation();
        }

        void SubscribeTouchEvent()
        {
            _inputEventProvider
                .TouchPosition
                .Skip(1)
                .Subscribe(pos => Model.OnScreenTouched(pos))
                .AddTo(this);
        }

        void SubscribeCharacterChange()
        {
            void SubscribeMoveAnimationEvent()
            {
                //! å√Ç¢_triggerÇÃçwì«ÇâèúÇ∑ÇÈ
#nullable enable
                _moveEnterSubject?.Dispose();
                _moveExitSubject?.Dispose();
#nullable disable

                var trigger = _view.Animator.GetBehaviour<ObservableStateMachineTrigger>();

                _moveEnterSubject =
                    trigger
                    .OnStateEnterAsObservable()
                    .Where(x => x.StateInfo.IsTag(MOVE))
                    .Subscribe(_ =>
                    {
                        Model.OnStartMoveToHex();
                        _view.ResetSpeed();
                    })
                    .AddTo(this);

                _moveExitSubject =
                trigger
                    .OnStateExitAsObservable()
                    .Where(x => x.StateInfo.IsTag(MOVE))
                    .Subscribe(_ =>
                    {
                        Model.OnFinishMoveToHex();
                        _view.UpdateMoveableIndicator();
                    })
                    .AddTo(this);
            }

            Model
                .CurCharacter
                .Subscribe(character =>
                {
                    _view.OnChangeCharacter(Model.CurSelectedCharacterIndex.Value);

                    Animator animator = null;
#nullable enable
                    if (!character?.TryGetComponent(out animator) ?? true) return;
#nullable disable
                    _view.SetAnimator(animator);
                    SubscribeMoveAnimationEvent();
                })
                .AddTo(this);
        }

        void SubscribeMove()
        {
            Model
                .HexMoveTo
                .Subscribe(hex =>
                {
                    if (hex == null) return;
                    _view.MoveToHex(hex);
                })
                .AddTo(this);

            Model
                .UpdateMoveableIndicator
                .Subscribe(_ =>
                {
                    _view.UpdateMoveableIndicator();
                })
                .AddTo(this);
        }

        void SubscribeSkillAnimation()
        {
            void SubscribeSkillAnimationEvent(string tag)
            {
                //! å√Ç¢_triggerÇÃçwì«ÇâèúÇ∑ÇÈ
#nullable enable
                _skillEnterSubject?.Dispose();
                _skillExitSubject?.Dispose();
#nullable disable

                var trigger = _view.Animator.GetBehaviour<ObservableStateMachineTrigger>();

                _skillEnterSubject = 
                trigger
                    .OnStateEnterAsObservable()
                    .Where(x => x.StateInfo.IsTag(tag))
                    .Subscribe(_ =>
                    {
                        Model.OnStartSkill();
                        _view.ResetTrigger(tag);
                    })
                    .AddTo(this);

                _skillExitSubject = 
                trigger
                    .OnStateExitAsObservable()
                    .Where(x => x.StateInfo.IsTag(tag))
                    .Subscribe(_ =>
                    {
                        _view.ResetRotation();
                        Model.OnFinishSkill();
                    })
                    .AddTo(this);
            }

            Model
                .SkillAnimationParam
                .Skip(1)
                .Subscribe(param =>
                {
                    SubscribeSkillAnimationEvent(param);
                    _view.SetRotation(Model.DuplicateSelectedCount * 60);
                    _view.SetTrigger(param);
                })
                .AddTo(this);
        }
    }
}
