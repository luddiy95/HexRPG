using System.Collections.Generic;
using UnityEngine;

public class BaseComponentCollectionFactoryComponent : AbstractCustomComponent, IComponentCollectionFactory
{
    IEnumerable<IInstaller> _installers;

    public override void Register(ICustomComponentCollection owner)
    {
        base.Register(owner);

        owner.RegisterInterface<IComponentCollectionFactory>(this);
    }

    public override void Initialize()
    {
        base.Initialize();

        Owner.QueryInterfaces(out _installers);
    }

    void Install(ICustomComponentCollection targetCollection)
    {
        foreach (var installer in _installers)
        {
            installer.InstallToComponentCollection(targetCollection);
        }
    }

    ICustomComponentCollection IComponentCollectionFactory.CreateComponentCollection(
        IEnumerable<ICustomComponent> components,
        System.Action<ICustomComponentCollection> onRegister
    )
    {
        onRegister += Install;
        return CustomComponentCollectionFactory.CreateComponentCollection(components, onRegister);
    }

    ICustomComponentCollection IComponentCollectionFactory.CreateComponentCollection(
        GameObject basePrefab,
        IEnumerable<ICustomComponent> components,
        System.Action<ICustomComponentCollection> onRegister
    )
    {
        onRegister += Install;
        return CustomComponentCollectionFactory.CreateComponentCollection(basePrefab, components, onRegister);
    }

    ICustomComponentCollection IComponentCollectionFactory.CreateComponentCollectionWithoutInstantiate(
        GameObject instanceObject,
        IEnumerable<ICustomComponent> components,
        System.Action<ICustomComponentCollection> onRegister
    )
    {
        onRegister += Install;
        return CustomComponentCollectionFactory.CreateComponentCollectionWithoutInstantiate(instanceObject, components, onRegister);
    }
}
