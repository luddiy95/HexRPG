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

    //! ゲームにおける基本機能のコレクション(シングルトン的に記載する機能)
    ICustomComponentCollection _commonComponentCollection;

    public override void Awake()
    {
        _commonComponentCollection = SetupBaseComponents();
        base.Awake();
    }

    //! 未初期化のtarget(ComponentCollection)に、基本機能が全て付いているComponentCollectionを生成する機能含めた基本機能全てを登録する
    protected override void InstallFeatures(ICustomComponentCollection target)
    {
        // 更新機能
        //! IUpdaterはtargetにはいらない(IUpdaterでの購読はSetupBaseComponentsで行われている)
        if (_commonComponentCollection.QueryInterface(out IUpdateObservable updateObservable))
        {
            target.RegisterInterface(updateObservable);
        }

        // DeltaTime
        if (_commonComponentCollection.QueryInterface(out IDeltaTime deltaTime))
        {
            target.RegisterInterface(deltaTime);
        }

        // 生成機能
        if (_commonComponentCollection.QueryInterface(out IComponentCollectionFactory factory))
        {
            target.RegisterInterface(factory);
        }

        //! ↓からCustomComponentBehaviour

        // バトルwatch
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
        // バトルJudge
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

        //! 基本機能が全て付いているComponentCollectionを生成する機能
        var factory = new BaseComponentCollectionFactoryComponent();

        commonComponents.Add(factory);

        //! 基本機能が全て付いているComponentCollectionを生成する機能含め基本機能が全て付いたComponentCollectionを生成(_basePrefabに付けた)
        var baseComponents = CustomComponentCollectionFactory.CreateComponentCollectionWithoutInstantiate(_basePrefab, commonComponents);

        // 更新起動
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
