using System.Collections.Generic;
using UnityEngine;

public interface IComponentCollectionFactory : IFeature
{
    ICustomComponentCollection CreateComponentCollection(
        IEnumerable<ICustomComponent> components = null,
        System.Action<ICustomComponentCollection> onRegister = null
    );

    ICustomComponentCollection CreateComponentCollection(
        GameObject basePrefab,
        IEnumerable<ICustomComponent> components = null,
        System.Action<ICustomComponentCollection> onRegister = null
    );

    ICustomComponentCollection CreateComponentCollectionWithoutInstantiate(
        GameObject instanceObject,
        IEnumerable<ICustomComponent> components = null,
        System.Action<ICustomComponentCollection> onRegister = null
    );
}
