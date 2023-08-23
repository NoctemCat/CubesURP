using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NativeArray<int> arr = new(10, Allocator.Persistent);
        Debug.Log($"{arr.IsCreated}");

        NativeArray<int> arr2 = arr;
        Debug.Log($"{arr2.IsCreated}");

        arr[4] = 10;
        Debug.Log($"{arr2[4]}");
        arr.Dispose();

        Debug.Log($"{arr.IsCreated}");
        Debug.Log($"{arr2.IsCreated}");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
