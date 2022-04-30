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
    public IObservable<Test> Value => _value; //-> nullÇ™î≠çsÇ≥ÇÍÇÈ
    
    readonly Subject<Test> _value = new Subject<Test>();

    public IObservable<int> Integer => _integer;
    readonly ISubject<int> _integer = new Subject<int>();

    private void Awake()
    {
        _value.OnNext(new Test(0));
        //_value.Value = new Test(0);
    }

    private void Start()
    {
        _value.OnNext(new Test(1));
        //_value.Value = new Test(1);

        _integer.OnNext(0);
        _integer.OnNext(0);
        _integer.OnNext(0);
        _integer.OnNext(1);
    }
}
