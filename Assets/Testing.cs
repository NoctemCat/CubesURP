using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    //List<TestingClass> testing = new(10);
    private event Action action;

    void Start()
    {
        action += Hello;
        action += HelloOther;
    }

    int count = 0;
    void Update()
    {

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log(GetSomePhrease());
            //count++;
            //action?.Invoke();

            //action -= Hello;

            //Debug.Log($"Called action {count} times");
        }
    }


    private void Hello()
    {
        Debug.Log("Hello");
    }

    private void HelloOther()
    {
        Debug.Log("Hello Other");
    }


    private string GetSomePhrease()
    {
        return "Some Phrase";
    }
}
