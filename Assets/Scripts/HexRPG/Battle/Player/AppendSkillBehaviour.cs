using UnityEngine;
using UniRx;
using Zenject;

namespace HexRPG.Battle.Player
{
    public enum AppendSkillType
    {
        RECOVERY_SP,
        RECOVERY_HP
    }

    public interface IAppendSkillObservable
    {
        AppendSkillType AppendSkillType { get; }
    }

    public class AppendSkillBehaviour : MonoBehaviour, IAppendSkillObservable
    {
        IScoreController _scoreController;
        IScoreObservable _scoreObservable;
        ICharacterInput _characterInput;
        IMemberObservable _memberObservable;

        AppendSkillType IAppendSkillObservable.AppendSkillType => _appendSkillType;
        [SerializeField] AppendSkillType _appendSkillType;

        [Inject]
        public void Construct(
            IScoreController scoreController,
            IScoreObservable scoreObservable,
            ICharacterInput characterInput,
            IMemberObservable memberObservable
        )
        {
            _scoreController = scoreController;
            _scoreObservable = scoreObservable;
            _characterInput = characterInput;
            _memberObservable = memberObservable;
        }

        void Start()
        {
            _characterInput.OnAppendSkill
                .Where(_ => _scoreObservable.CurScore.Value >= _scoreObservable.ScoreMax)
                .Subscribe(_ =>
                {
                    ExecuteAppendSkill();
                    _scoreController.Update(-_scoreObservable.ScoreMax);
                })
                .AddTo(this);
        }

        void ExecuteAppendSkill()
        {
            switch (_appendSkillType)
            {
                case AppendSkillType.RECOVERY_SP:
                    _memberObservable.MemberList.ForEach(memberOwner =>
                    {
                        if(memberOwner.DieObservable.IsDead.Value == false)
                        {
                            var skillPoint = memberOwner.SkillPoint;
                            skillPoint.Update(skillPoint.Max);
                        }
                    });
                    break;
                case AppendSkillType.RECOVERY_HP:
                    _memberObservable.MemberList.ForEach(memberOwner =>
                    {
                        if (memberOwner.DieObservable.IsDead.Value == false)
                        {
                            var health = memberOwner.Health;
                            health.Update(health.Max);
                        }
                    });
                    break;
            }
        }
    }
}
