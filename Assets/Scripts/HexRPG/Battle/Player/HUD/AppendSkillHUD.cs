using UniRx;
using Zenject;
using UnityEngine;
using UnityEngine.UI;

namespace HexRPG.Battle.Player.HUD
{
    using Battle.HUD;

    public class AppendSkillHUD : MonoBehaviour, ICharacterHUD
    {
        BattleData _battleData;
        IScoreObservable _scoreObservable;

        [SerializeField] Image _btnAppendSkill;
        [SerializeField] Image _iconAppendSkill;

        [SerializeField] Sprite _enableBackground;
        [SerializeField] Sprite _disableBackground;

        int _scoreMax;

        [Inject]
        public void Construct(
            BattleData battleData,
            IScoreObservable scoreObservable
        )
        {
            _battleData = battleData;
            _scoreObservable = scoreObservable;
        }

        void ICharacterHUD.Bind(ICharacterComponentCollection character)
        {
            if (character is IPlayerComponentCollection playerOwner)
            {
                var appendSkillType = playerOwner.AppendSkillObservable.AppendSkillType;
                if (_battleData.appendSkillIconMap.Table.TryGetValue(appendSkillType, out Sprite iconAppendSkillSprite))
                {
                    _iconAppendSkill.sprite = iconAppendSkillSprite;
                }

                _scoreMax = _scoreObservable.ScoreMax;
                _scoreObservable.CurScore
                    .Subscribe(score =>
                    {
                        _btnAppendSkill.sprite = (score >= _scoreMax) ? _enableBackground : _disableBackground;
                    })
                    .AddTo(this);
            }

        }
    }
}
