using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Noise))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Noise mapGen = (Noise)target;

        if (GUILayout.Button("Generate"))
        {
            //mapGen.GenerateMap();
        }
    }
}