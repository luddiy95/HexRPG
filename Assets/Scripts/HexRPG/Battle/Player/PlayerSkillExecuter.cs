using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player
{
    using Stage;
    using Battle.Skill;

    public class PlayerSkillExecuter : ISkillController, ISkillObservable, IDisposable
    {
        ITransformController _transformController;
        IMemberObservable _memberObservable;
        ISelectSkillObservable _selectSkillObservable;
        IStageController _stageController;

        CompositeDisposable _disposables = new CompositeDisposable();

        IReadOnlyReactiveProperty<Hex[]> ISkillObservable.OnSkillAttack => null;

        IObservable<Unit> ISkillObservable.OnFinishSkill => _onFinishSkill;
        readonly ISubject<Unit> _onFinishSkill = new Subject<Unit>();

        public PlayerSkillExecuter(
            ITransformController transformController,
            IMemberObservable memberObservable,
            ISelectSkillObservable selectSkillObservable,
            IStageController stageController
        )
        {
            _transformController = transformController;
            _memberObservable = memberObservable;
            _selectSkillObservable = selectSkillObservable;
            _stageController = stageController;
        }

        ISkillComponentCollection ISkillController.StartSkill(int index, Hex landedHex, int skillRotation)
        {
            _transformController.RotationAngle = _selectSkillObservable.SelectedSkillRotation - _transformController.DefaultRotation;

            var runningSkill = 
                _memberObservable.CurMember.Value.SkillController.StartSkill(
                    index, 
                    _transformController.GetLandedHex(), 
                    _selectSkillObservable.SelectedSkillRotation
                );
            _disposables.Clear();
            runningSkill.SkillObservable.OnSkillAttack
                .Skip(1)
                .Subscribe(attackRange =>
                {
                    //TODO: 攻撃着弾直後にskillRange内に生きている敵がいるかどうか->いなければLiberate
                    //TODO: 敵の生死判定をまだ決定していないため、とりあえず敵の有無/生死にかかわらずLiberate
                    _stageController.Liberate(attackRange, true);
                    //TODO: 【ここから】
                    //TODO: 多段Skillを想定すると、OnFinishAttackをUnitではなくHex[]にしてでその時攻撃した範囲を取ってきてList<Hex[]>のキャッシュに追加し、
                    //TODO:  OnFinishSkill時にキャッシュされたList<Hex[]>のそれぞれのHex[]に対してLiberateを行う(中断にも対応できる)
                    //TODO: SkillSettingのRangeは多段の攻撃範囲全て網羅するようにして(Indicateするときも網羅した範囲)、多段の各攻撃の範囲はTimelineのトラックで取得するようにする
                    //TODO: 網羅させるのはTimelineから多段の各攻撃範囲を読み取ってそれを統合すれば良い
                })
                .AddTo(_disposables);
            runningSkill.SkillObservable.OnFinishSkill
                .Subscribe(_ =>
                {
                    _transformController.RotationAngle = 0;
                    _onFinishSkill.OnNext(Unit.Default);
                }).AddTo(_disposables);

            return runningSkill;
        }

        void IDisposable.Dispose()
        {
            _disposables.Dispose();
        }
    }
}
