using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// https://forum.unity.com/threads/solved-but-unhappy-scriptableobject-awake-never-execute.488468/#post-4483018
/// And when you inherit from it, you implement the OnBegin and OnEnd methods. 
/// It runs on starting unity editor, on entering/exiting playmode and on 
/// starting your built application.
/// </summary>
//[InitializeOnLoad]
//public abstract class ManagedObject : ScriptableObject
//{
//    abstract protected void OnBegin();
//    abstract protected void OnEnd();

//#if UNITY_EDITOR
//    protected void OnEnable()
//    {
//        EditorApplication.playModeStateChanged += OnPlayStateChange;
//    }

//    protected void OnDisable()
//    {
//        EditorApplication.playModeStateChanged -= OnPlayStateChange;
//    }

//    void OnPlayStateChange(PlayModeStateChange state)
//    {
//        if (state == PlayModeStateChange.EnteredPlayMode)
//        {
//            OnBegin();
//        }
//        else if (state == PlayModeStateChange.ExitingPlayMode)
//        {
//            OnEnd();
//        }
//    }
//#else
//    protected void OnEnable()
//    {
//        OnBegin();
//    }

//    protected void OnDisable()
//    {
//        OnEnd();
//    }
//#endif
//}
