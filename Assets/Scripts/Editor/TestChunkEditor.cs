using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

//[CustomEditor(typeof(TestChunkDisplay))]
//public class TestChunkEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();
//        TestChunkDisplay display = (TestChunkDisplay)target;

//        if (GUILayout.Button("Generate"))
//        {
//            display.DrawTexture();
//        }
//    }

//    private void OnSceneGUI()
//    {
//        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

//        //Mouse.current.
//        //Event.current.
//        if (Physics.Raycast(ray, out RaycastHit hit))
//        {
//            // do stuff
//            Debug.Log(hit.collider.gameObject.name);
//            Debug.Log(hit.textureCoord);

//            //hit.
//        }
//    }
//}