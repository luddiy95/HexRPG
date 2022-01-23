using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

using HexRPG;
using HexRPG.Battle;
using HexRPG.Battle.Stage;

public class BaseComponentInstaller : ComponentInstaller
{
    [SerializeField] GameObject _basePrefab;

    //! �Q�[���ɂ������{�@�\�̃R���N�V����(�V���O���g���I�ɋL�ڂ���@�\)
    ICustomComponentCollection _commonComponentCollection;

    public override void Awake()
    {
        _commonComponentCollection = SetupBaseComponents();
        base.Awake();
    }

    //! ����������target(ComponentCollection)�ɁA��{�@�\���S�ĕt���Ă���ComponentCollection�𐶐�����@�\�܂߂���{�@�\�S�Ă�o�^����
    protected override void InstallFeatures(ICustomComponentCollection target)
    {
        // �X�V�@�\
        //! IUpdater��target�ɂ͂���Ȃ�(IUpdater�ł̍w�ǂ�SetupBaseComponents�ōs���Ă���)
        if (_commonComponentCollection.QueryInterface(out IUpdateObservable updateObservable))
        {
            target.RegisterInterface(updateObservable);
        }

        // DeltaTime
        if (_commonComponentCollection.QueryInterface(out IDeltaTime deltaTime))
        {
            target.RegisterInterface(deltaTime);
        }

        // �����@�\
        if (_commonComponentCollection.QueryInterface(out IComponentCollectionFactory factory))
        {
            target.RegisterInterface(factory);
        }

        //! ������CustomComponentBehaviour

        // �o�g��watch
        if (_commonComponentCollection.QueryInterface(out IBattleObservable battleObservable))
        {
            target.RegisterInterface(battleObservable);
        }

        // Stage
        if (_commonComponentCollection.QueryInterface(out IStageController stageController))
        {
            target.RegisterInterface(stageController);
        }

        /*
        // �o�g��Judge
        if (_commonComponentCollection.QueryInterface(out IBattleJudge battleJudge))
        {
            target.RegisterInterface(battleJudge);
        }
        */
    }

    ICustomComponentCollection SetupBaseComponents()
    {
        var commonComponents = new List<ICustomComponent>();

        commonComponents.Add(this);

        commonComponents.AddRange(new List<ICustomComponent>
        {
            new UpdateFeature(),
            new StageController(),
            new DeltaTime()
        });

        //! ��{�@�\���S�ĕt���Ă���ComponentCollection�𐶐�����@�\
        var factory = new BaseComponentCollectionFactoryComponent();

        commonComponents.Add(factory);

        //! ��{�@�\���S�ĕt���Ă���ComponentCollection�𐶐�����@�\�܂ߊ�{�@�\���S�ĕt����ComponentCollection�𐶐�(_basePrefab�ɕt����)
        var baseComponents = CustomComponentCollectionFactory.CreateComponentCollectionWithoutInstantiate(_basePrefab, commonComponents);

        // �X�V�N��
        if (baseComponents.QueryInterface(out IUpdater updater))
        {
            this.UpdateAsObservable()
                .Subscribe(_ => updater.FireUpdateStreams())
                .AddTo(this);
        }

        return baseComponents;
    }

    public void OnDestroy()
    {
        _commonComponentCollection.Dispose();
    }
}
