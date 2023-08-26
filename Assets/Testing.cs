using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    List<TestingClass> testing = new(10);

    void Start()
    {
        //string getText()
        //{

        //    return $"Timer tooltip \n{_timer}";
        //}
        //TooltipScreenSpaceUI.ShowTooltip_Static(getText);
        testing.Add(new TestingClass());
        testing.Add(new TestingClass());
        testing.Add(new Derived());
        testing.Add(new Derived2("Derived 2 1"));
        testing.Add(new Derived2("Other text"));
        testing.Add(new Derived());
        testing.Add(new TestingClass());
        testing.Add(new Derived());
        testing.Add(new Derived2("Derived 2 3"));
        testing.Add(new Derived());

        foreach (TestingClass item in testing)
        {
            if (item is ITestInterface testInterface)
            {
                //Debug.Log(testInterface.GetText());
            }
        }
    }

    void Update()
    {
    }
}

public interface ITestInterface
{
    string GetText();
}

public class TestingClass
{
    public int type = 1;
    public TestingClass() { }
}

public class Derived : TestingClass
{
    public Derived() { type = 2; }
}
public class Derived2 : TestingClass, ITestInterface
{
    public string Holder;
    public Derived2(string text)
    {
        type = 3;
        Holder = text;
    }

    public string GetText()
    {
        return Holder;
    }
}