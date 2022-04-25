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
                    //TODO: �U�����e�����skillRange���ɐ����Ă���G�����邩�ǂ���->���Ȃ����Liberate
                    //TODO: �G�̐���������܂����肵�Ă��Ȃ����߁A�Ƃ肠�����G�̗L��/�����ɂ�����炸Liberate
                    _stageController.Liberate(attackRange, true);
                    //TODO: �y��������z
                    //TODO: ���iSkill��z�肷��ƁAOnFinishAttack��Unit�ł͂Ȃ�Hex[]�ɂ��Ăł��̎��U�������͈͂�����Ă���List<Hex[]>�̃L���b�V���ɒǉ����A
                    //TODO:  OnFinishSkill���ɃL���b�V�����ꂽList<Hex[]>�̂��ꂼ���Hex[]�ɑ΂���Liberate���s��(���f�ɂ��Ή��ł���)
                    //TODO: SkillSetting��Range�͑��i�̍U���͈͑S�Ėԗ�����悤�ɂ���(Indicate����Ƃ����ԗ������͈�)�A���i�̊e�U���͈̔͂�Timeline�̃g���b�N�Ŏ擾����悤�ɂ���
                    //TODO: �ԗ�������̂�Timeline���瑽�i�̊e�U���͈͂�ǂݎ���Ă���𓝍�����Ηǂ�
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
