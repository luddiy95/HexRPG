using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace HexRPG.Battle.Stage
{
    using Player;

    public class StagePresenter : MonoBehaviour
    {
        StageView _view;
        PlayerModel _playerModel;

        List<Hex> _attackIndicatedHexList = new List<Hex>();

        public void Init(PlayerModel playerModel)
        {
            _view = GetComponent<StageView>();
            _playerModel = playerModel;

            SubscribeSelectSkillPanelEvent();
            SubscribePlayerLiberation();
        }

        void SubscribeSelectSkillPanelEvent()
        {
            _playerModel
                .CurSelectedSkillIndex
                .Skip(1)
                .Subscribe(index =>
                {
                    /*
                    _view.ResetAttackIndicatedHexList(_attackIndicatedHexList);
                    _attackIndicatedHexList.Clear();
                    List<Vector2> range = _playerModel.CurMemberSkillRange(index);
                    range
                        .Select(dir =>
                        {
                            Vector3 position = _playerModel.LandedHex.transform.position +
                            Quaternion.AngleAxis(60 * _playerModel.DuplicateSelectedCount, Vector3.up) * (_view._dirX * dir.x + _view._dirZ * dir.y);
                            return BattlePreference.Instance.GetAnyLandedHex(position);
                        })
                        .Where(rangeHex => rangeHex != null).ToList()
                        .ForEach(rangeHex =>
                        {
                            _view.SetAttackIndicated(rangeHex);
                            _attackIndicatedHexList.Add(rangeHex);
                        });
                    */
                })
                .AddTo(this);

            _playerModel
                .ClearSelectedSkillIndex
                .Subscribe(_ =>
                {
                    _view.ResetAttackIndicatedHexList(_attackIndicatedHexList);
                })
                .AddTo(this);
        }

        void SubscribePlayerLiberation()
        {
            _playerModel
                .LiberateHex
                .Subscribe(_ =>
                {
                    _view.LiberateHexList(_attackIndicatedHexList);
                    _attackIndicatedHexList.Clear();
                })
                .AddTo(this);
        }
    }
}
