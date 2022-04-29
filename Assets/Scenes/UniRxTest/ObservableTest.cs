using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class ObservableTest : MonoBehaviour
{
    public class Test
    {
        public int test;
        public Test(int test)
        {
            this.test = test;
        }
    }

    //public IReadOnlyReactiveProperty<Test> Value => _value.Skip(1).ToReadOnlyReactiveProperty();
    public IReadOnlyReactiveProperty<Test> Value => _value; //-> nullÇ™î≠çsÇ≥ÇÍÇÈ
    
    readonly IReactiveProperty<Test> _value = new ReactiveProperty<Test>();

    private void Awake()
    {
        _value.Value = new Test(0);
    }

    private void Start()
    {
        _value.Value = new Test(1);
    }
}
