using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeAttributes))]
public class BiomeAttributesEditor : Editor
{
    private BiomeAttributes _biomeAttributes;
    private Texture2D _noiseTexture;

    private void OnEnable()
    {
        _biomeAttributes = (BiomeAttributes)target;
        _noiseTexture = new Texture2D(128, 128)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        UpdateTexture();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (_biomeAttributes.needsAutoUpdate)
        {
            UpdateTexture();
            _biomeAttributes.AutoUpdate();
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();

        GUILayoutOption[] options = new GUILayoutOption[2];
        options[0] = GUILayout.Width(_biomeAttributes.previewWidth);
        options[1] = GUILayout.Height(_biomeAttributes.previewWidth);
        GUILayout.Label("", options);
        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), _noiseTexture);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void UpdateTexture()
    {
        if (_biomeAttributes.noise == null) return;
        BiomeHelper.FillTexture(
            ref _noiseTexture,
            128,
            128,
            _biomeAttributes.noise.seed,
            _biomeAttributes.noise.octaves,
            _biomeAttributes.terrainScale / _biomeAttributes.chunksShownWidth,
            _biomeAttributes.noise.offset
        );
    }
}